namespace RentConnect.Models.Context
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using RentConnect.Models.Entities;
    using RentConnect.Models.Entities.Documents;
    using RentConnect.Models.Entities.Landlords;
    using RentConnect.Models.Entities.Payments;
    using RentConnect.Models.Entities.Properties;
    using RentConnect.Models.Entities.Tenants;
    using RentConnect.Models.Entities.TicketTracking;

    public class ApiContext : IdentityDbContext<ApplicationUser, ApplicationUserIdentityRole, long>
    {
        public ApiContext(string connectionString) : this(new DbContextOptionsBuilder<ApiContext>().UseSqlServer(connectionString).Options)
        {
            // This constructor is for support to use this context in LinqPad
        }

        public ApiContext(DbContextOptions<ApiContext> options) : base(options)
        {
        }

        public virtual DbSet<Landlord> Landlord { get; set; }
        public virtual DbSet<Property> Property { get; set; }
        public virtual DbSet<Document> Document { get; set; }

        public virtual DbSet<Tenant> Tenant { get; set; }

        public virtual DbSet<Ticket> Ticket { get; set; }
        public virtual DbSet<TicketStatus> TicketStatus { get; set; }
        public virtual DbSet<TicketComment> TicketComment { get; set; }
        public virtual DbSet<TicketAttachment> TicketAttachment { get; set; }

        public virtual DbSet<RentPayment> RentPayment { get; set; }
        public virtual DbSet<RentLatePaymentCharge> RentLatePaymentCharge { get; set; }
        public virtual DbSet<RentPaymentHistory> RentPaymentHistory { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ✅ Set default schema to dbo
            modelBuilder.HasDefaultSchema("dbo");

            // Let Identity configure itself
            base.OnModelCreating(modelBuilder);

            // ✅ Map custom entities
            modelBuilder.Entity<Landlord>().ToTable("Landlord", "dbo");
            modelBuilder.Entity<Property>().ToTable("Property", "dbo");
            modelBuilder.Entity<Document>().ToTable("Document", "dbo");
            modelBuilder.Entity<Tenant>().ToTable("Tenant", "dbo");
            modelBuilder.Entity<Ticket>().ToTable("Ticket", "dbo");
            modelBuilder.Entity<TicketStatus>().ToTable("TicketStatus", "dbo");
            modelBuilder.Entity<TicketComment>().ToTable("TicketComment", "dbo");
            modelBuilder.Entity<TicketAttachment>().ToTable("TicketAttachment", "dbo");
            modelBuilder.Entity<RentPayment>().ToTable("RentPayment", "dbo");
            modelBuilder.Entity<RentLatePaymentCharge>().ToTable("RentLatePaymentCharge", "dbo");
            modelBuilder.Entity<RentPaymentHistory>().ToTable("RentPaymentHistory", "dbo");

            // -------------------------
            // 🔗 Relationships
            // -------------------------

            // Landlord → Property (1:N)
            modelBuilder.Entity<Property>()
                .HasOne(p => p.Landlord)
                .WithMany(l => l.Properties)
                .HasForeignKey(p => p.LandlordId)
                .OnDelete(DeleteBehavior.Restrict);

            // Landlord → Tenant (1:N)
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.Landlord)
                .WithMany(l => l.Tenants)
                .HasForeignKey(t => t.LandlordId)
                .OnDelete(DeleteBehavior.Cascade);



            // Property → Tenant (1:N)
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.Property)
                .WithMany(p => p.Tenants)
                .HasForeignKey(t => t.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tenant → Tickets (1:N)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Tenant)
                .WithMany(te => te.Tickets)
                .HasForeignKey(t => t.TenantId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ticket → StatusHistory (1:N)
            modelBuilder.Entity<TicketStatus>()
                .HasOne(ts => ts.Ticket)
                .WithMany(t => t.StatusHistory)
                .HasForeignKey(ts => ts.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ticket → Property (Many-to-1)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Property)
                .WithMany()
                .HasForeignKey(t => t.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket → Comments (1:N)
            modelBuilder.Entity<TicketComment>()
                .HasOne(tc => tc.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(tc => tc.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // TicketComment → Attachments (1:N)
            modelBuilder.Entity<TicketAttachment>()
                .HasOne(ta => ta.Comment)
                .WithMany(tc => tc.Attachments)
                .HasForeignKey(ta => ta.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser → Roles
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(x => x.Roles)
                .WithOne()
                .HasForeignKey(ur => ur.UserId);

            // ApplicationUser → Claims
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(x => x.Claims)
                .WithOne()
                .HasForeignKey(ur => ur.UserId);

            // -------------------------
            // ⚠️ Documents (polymorphic)
            // -------------------------
            // If you don’t have a discriminator, you must ignore them here,
            // OR later create a DocumentOwner table to normalize.
            modelBuilder.Entity<Landlord>().Ignore(l => l.Documents);
            modelBuilder.Entity<Property>().Ignore(p => p.Documents);
            modelBuilder.Entity<Tenant>().Ignore(t => t.Documents);
        }
    }
}