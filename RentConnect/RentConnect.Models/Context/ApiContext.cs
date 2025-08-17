namespace RentConnect.Models.Context
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using RentConnect.Models.Entities;
    using RentConnect.Models.Entities.Documents;
    using RentConnect.Models.Entities.Landlords;
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

        public virtual DbSet<TenantChildren> TenantChildren { get; set; } // no where we featch Tenant children to show in ui ,it will keep record for who are below 18 year,if future require to make relationship with Tenant then add  TenantChildren as property and make foreginkey rleationship to tenantgroup id

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ✅ Set default schema to dbo
            modelBuilder.HasDefaultSchema("dbo");

            // Let Identity configure itself
            base.OnModelCreating(modelBuilder);

            // ✅ Map your custom entities
            modelBuilder.Entity<Landlord>().ToTable("Landlord", "dbo");
            modelBuilder.Entity<Property>().ToTable("Property", "dbo");
            modelBuilder.Entity<Document>().ToTable("Document", "dbo");
            modelBuilder.Entity<Tenant>().ToTable("Tenant", "dbo");
            modelBuilder.Entity<Ticket>().ToTable("Ticket", "dbo");
            modelBuilder.Entity<TicketStatus>().ToTable("TicketStatus", "dbo");
            modelBuilder.Entity<TenantChildren>().ToTable("TenantChildren", "dbo");

            // Relationships

            // Landlord → Property
            modelBuilder.Entity<Property>()
                .HasOne(p => p.Landlord)
                .WithMany(l => l.Properties)
                .HasForeignKey(p => p.LandlordId)
                .OnDelete(DeleteBehavior.Restrict);

            // Landlord → Documents (polymorphic, no FK)
            modelBuilder.Entity<Landlord>()
                .Ignore(l => l.Documents);

            // Property → Documents (polymorphic, no FK)
            modelBuilder.Entity<Property>()
                .Ignore(p => p.Documents);

            // Document → Owner (polymorphic)
            modelBuilder.Entity<Document>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<Tenant>()
                    .HasOne(a => a.User)
                    .WithMany()
                    .HasPrincipalKey(x => x.Id)
                    .HasForeignKey(c => c.Id);

            modelBuilder.Entity<Tenant>()
               .HasOne(a => a.Property)
               .WithMany()
               .HasPrincipalKey(x => new { x.Id, x.Landlord })
               .HasForeignKey(c => new { c.PropertyId, c.LandLordId });

            // Property → Documents (polymorphic, no FK)
            modelBuilder.Entity<Tenant>()
                .Ignore(p => p.Documents);

            modelBuilder.Entity<Ticket>()
                     .HasMany(a => a.Status)
                     .WithOne()
                     .HasPrincipalKey(x => x.Id)
                     .HasForeignKey(c => c.TicketId);

            modelBuilder.Entity<Ticket>()
               .HasOne(a => a.Tenant)
               .WithMany()
               .HasPrincipalKey(x => x.Id)
               .HasForeignKey(c => c.TenantId);

            modelBuilder.Entity<Ticket>()
              .HasOne(a => a.Property)
              .WithMany()
              .HasPrincipalKey(x => x.Id)
              .HasForeignKey(c => c.PropertyId);

            modelBuilder.Entity<ApplicationUser>()
                        .HasMany(x => x.Roles)
                        .WithOne()
                        .HasPrincipalKey(x => x.Id)
                        .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(x => x.Claims)
                .WithOne()
                .HasPrincipalKey(x => x.Id)
                .HasForeignKey(ur => ur.UserId);
        }
    }
}