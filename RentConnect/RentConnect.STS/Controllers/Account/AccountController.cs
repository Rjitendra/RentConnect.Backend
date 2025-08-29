namespace RentConnect.STS.Controllers
{
    using Duende.IdentityModel;
    using Duende.IdentityServer;
    using Duende.IdentityServer.Events;
    using Duende.IdentityServer.Extensions;
    using Duende.IdentityServer.Services;
    using Duende.IdentityServer.Stores;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Html;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;
    using RentConnect.Models.Configs;
    using RentConnect.Models.Context;
    using RentConnect.Models.Dtos;
    using RentConnect.Models.Dtos.Document;
    using RentConnect.Models.Entities;
    using RentConnect.Models.Entities.Landlords;
    using RentConnect.Models.Enums;
    using RentConnect.Services.Interfaces;
    using RentConnect.STS.Config;
    using RentConnect.STS.Extensions;
    using Stripe;
    using Stripe.Checkout;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;

    [SecurityHeaders]
    public class AccountController : Controller
    {
        public AccountController(
            ApiContext apiContext,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationUserIdentityRole> roleManager,
            IIdentityServerInteractionService interaction,
            IAuthenticationSchemeProvider schemeProvider,
            IPersistedGrantStore persistedGrantStore,
            IEventService events,
            IMailService mailer,
            IOptions<STS.Config.ClientApplicationSettings> clientSettings,
             StripeSetting stripeSetting,
             ClientApplicationSettings clientApplicationSettings
            )
        {
            ApiContext = apiContext;
            UserManager = userManager;
            SignInManager = signInManager;
            Interaction = interaction;
            SchemeProvider = schemeProvider;
            PersistedGrantStore = persistedGrantStore;
            Events = events;
            Mailer = mailer;
            ClientSettings = clientSettings;
            RoleManager = roleManager;
            this._stripeSetting = stripeSetting;
            this._clientApplicationSettings = clientApplicationSettings;
        }

        private ApiContext ApiContext { get; }

        private readonly IEventService Events;
        private IIdentityServerInteractionService Interaction { get; }
        private IMailService Mailer { get; set; }
        private IPersistedGrantStore PersistedGrantStore { get; }
        private IAuthenticationSchemeProvider SchemeProvider { get; }
        private SignInManager<ApplicationUser> SignInManager { get; }
        private UserManager<ApplicationUser> UserManager { get; }
        private IOptions<ClientApplicationSettings> ClientSettings { get; }

        private readonly RoleManager<ApplicationUserIdentityRole> RoleManager;

        private readonly StripeSetting _stripeSetting;

        private readonly ClientApplicationSettings _clientApplicationSettings;

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var context = await Interaction.GetAuthorizationContextAsync(returnUrl);

            // Redirect to the client Application URL to get the Client's context so when users sign in without a context they aren't left in "limbo" state in the STS site.
            if (context == null)
                return Redirect(this.ClientSettings.Value.AngularBaseUrl);

            return View(await BuildLoginViewModelAsync(returnUrl));
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model, string button)
        {
            if (button != "login")
            {
                // the user clicked the "cancel" button
                var context = await Interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                if (context != null)
                {
                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    return Redirect(model.ReturnUrl);
                }
                else
                {
                    // since we don't have a valid context, then we just go back to the home page
                    return Redirect("~/");
                }
            }

            if (ModelState.IsValid)
            {
                var result = await SignInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberLogin, lockoutOnFailure: true);

                var user = await UserManager.FindByNameAsync(model.Username);

                if (result.Succeeded)
                {
                    if (user != null && !user.IsEnabled)
                    {
                        var userClaims = await UserManager.GetClaimsAsync(user);
                        var roles = await UserManager.GetRolesAsync(user);
                        if (roles[0] == ApplicationUserRole.Landlord.ToString())
                        {
                            await Events.RaiseAsync(new UserLoginFailureEvent(model.Username, "Your account has been disable."));
                            return RedirectToAction(nameof(Charge));
                        }
                    }

                    await Events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id.ToString(), user.UserName));

                    // make sure the returnUrl is still valid, and if so redirect back to authorize
                    // endpoint or a local page the IsLocalUrl check is only necessary if you want to
                    // support additional local pages, otherwise IsValidReturnUrl is more strict
                    if (Interaction.IsValidReturnUrl(model.ReturnUrl) || Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return Redirect("~/");
                }
                if (user != null && !await UserManager.IsEmailConfirmedAsync(user))
                {
                    await Events.RaiseAsync(new UserLoginFailureEvent(model.Username, "Email verify require."));
                    // Email not confirmed
                    // Return an appropriate response, such as displaying a message to confirm the email address
                    return RedirectToAction(nameof(EmailConfirmationRequired));
                }
                if (result.IsLockedOut)
                {
                    await Events.RaiseAsync(new UserLoginFailureEvent(model.Username, "User locked out."));

                    return this.RedirectToAction(nameof(Lockout));
                }

                await Events.RaiseAsync(new UserLoginFailureEvent(model.Username, "Invalid credentials"));

                ModelState.AddModelError("", AccountOptions.InvalidCredentialsErrorMessage);
            }

            // something went wrong, show form with error
            var vm = await BuildLoginViewModelAsync(model);
            return View(vm);
        }

        [HttpGet]
        public IActionResult EmailConfirmationRequired()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EmailConfirmationRequired(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await this.UserManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "User Not Found");
                    return View(model);
                }

                if ((await this.UserManager.IsEmailConfirmedAsync(user)))
                {
                    ModelState.AddModelError("", "Looks like your email has already been verified.");
                    return View(model);
                }

                var claims = await UserManager.GetClaimsAsync(user);
                string name = claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value;

                var code = await this.UserManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = this.Url.EmailConfirmationLink(user.Id, code, this.Request.Scheme);
                string link = HtmlEncoder.Default.Encode(callbackUrl);
                string htmlString = $@"<!DOCTYPE html>
                            <html>
                              <head>
                                <meta charset='UTF-8'>
                                <title>Welcome to RentConnect</title>
                              </head>
                              <body style='margin:0; padding:0; font-family: Arial, sans-serif; background-color:#f4f6f8;'>
                                <table width='100%' cellpadding='0' cellspacing='0'>
                                  <tr>
                                    <td align='center' style='padding:40px 0; background-color:#1a1a1a;'>
                                      <h1 style='color:#ffffff; margin:0; font-size:28px;'>RentConnect</h1>
                                    </td>
                                  </tr>
                                  <tr>
                                    <td align='center'>
                                      <table width='600' cellpadding='20' cellspacing='0' style='background:#ffffff; border-radius:8px; margin-top:30px; box-shadow:0 4px 12px rgba(0,0,0,0.1);'>
                                        <tr>
                                          <td>
                                            <p style='font-size:16px; color:#333;'>Dear <strong>{name}</strong>,</p>
                                            <p style='font-size:16px; color:#333;'>Welcome to <strong>RentConnect</strong>! To access your account, please confirm your email address by clicking the button below:</p>
                                            <p style='text-align:center; margin:30px 0;'>
                                              <a href='{link}' style='display:inline-block; padding:12px 24px; background-color:#2c3e50; color:#ffffff; text-decoration:none; border-radius:6px; font-size:16px; font-weight:bold;'>Verify My Account</a>
                                            </p>
                                            <p style='font-size:14px; color:#666;'>If the button doesn’t work, copy and paste this link into your browser:</p>
                                            <p style='word-break:break-all; color:#2c3e50; font-size:13px;'>{link}</p>
                                            <p style='margin-top:30px; font-size:16px; color:#333;'>Kind regards,<br><strong>RentConnect Admin</strong></p>
                                            <hr style='border:none; border-top:1px solid #ddd; margin:20px 0;' />
                                            <p style='font-size:13px; color:#888;'>
                                              Customer Service Department<br />
                                              E-mail: <a href='mailto:admin@RentConnect.me' style='color:#2c3e50;'>admin@RentConnect.me</a>
                                            </p>
                                          </td>
                                        </tr>
                                      </table>
                                    </td>
                                  </tr>
                                  <tr>
                                    <td align='center' style='padding:20px; font-size:12px; color:#aaa;'>
                                      &copy; {DateTime.Now.Year} RentConnect. All rights reserved.
                                    </td>
                                  </tr>
                                </table>
                              </body>
                            </html>";

                var mailObj = new MailRequestDto()
                {
                    ToEmail = user.Email,
                    Subject = "RentConnect - Email Verification",
                    Body = htmlString,
                    Attachments = null
                };
                await this.Mailer.SendEmailAsync(mailObj);
                return View("ResendConfirmationEmail");
            }
            return View(model);
        }

        /// <summary>
        /// Show charge page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Charge()
        {
            // Retrieve the user's identity from the authentication context
            var user = HttpContext.User;

            if (user.Identity.IsAuthenticated)
            {
                var subjectId = this.User.Claims.Single(x => x.Type == "sub").Value;
                int landlordIdInInt = int.Parse(subjectId);

                var landlordProfile = await this.ApiContext.Landlord.Where(x => x.Id == landlordIdInInt).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                PaymentRequest pr = new PaymentRequest()
                {
                    Amount = 40,
                    landlorId = landlordIdInInt,
                    TenantGroup = 0
                };
                return View(pr);
            }

            return this.RedirectToAction(nameof(Login));
        }

        [HttpPost]
        public IActionResult Charge(PaymentRequest paymentRequest)
        {
            // Retrieve the user's identity from the authentication context
            var user = HttpContext.User;

            if (user.Identity.IsAuthenticated)
            {
                var subjectId = this.User.Claims.Single(x => x.Type == "sub").Value;
                int landlordIdInInt = int.Parse(subjectId);

                StripeConfiguration.ApiKey = _stripeSetting.ApiKey;

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                                {
                                    "card"
                                },
                    LineItems = new List<SessionLineItemOptions>
                                            {
                                                new SessionLineItemOptions
                                                {
                                                    PriceData = new SessionLineItemPriceDataOptions
                                                    {
                                                        Currency = "GBP", // change currency to GBP
                                                        UnitAmount = 40*100,
                                                        ProductData = new SessionLineItemPriceDataProductDataOptions
                                                        {
                                                            Name = "Renew Account"
                                                        }
                                                    },
                                                    Quantity = 1
                                                },
                },
                    PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        Metadata = new Dictionary<string, string>
                                                    {
                                                        { "landlordId", landlordIdInInt.ToString() }
                                                    },
                    },

                    Mode = "payment",
                    SuccessUrl = $"{this._clientApplicationSettings.BaseUrl}Account/Success?amount={paymentRequest.Amount}",
                    CancelUrl = $"{this._clientApplicationSettings.BaseUrl}Account/Cancel?amount={paymentRequest.Amount}",
                };

                var service = new SessionService();

                var accountLink = service.Create(options);
                var response = new AccountLinkResponse
                {
                    Object = accountLink.Object,
                    Created = accountLink.Created,
                    ExpiresAt = accountLink.ExpiresAt,
                    Url = accountLink.Url
                };

                return Redirect(response.Url);
            }
            return this.RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Success(int amount)
        {
            var model = new PaymentRequest
            {
                Amount = amount
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult Cancel(int amount)
        {
            var model = new PaymentRequest
            {
                Amount = amount
            };

            return View(model);
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // build a model so the logout page knows what to display
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then we
                // don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            // build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                // logout of the MVC app
                await this.SignInManager.SignOutAsync();
                await this.HttpContext.SignOutAsync();

                // revoke all user grants effectively logs the user out of all clients
                var subjectId = this.User.Claims.Single(x => x.Type == "sub").Value;
                var c = new PersistedGrantFilter { SubjectId = subjectId };
                var grants = await this.PersistedGrantStore.GetAllAsync(c);
                foreach (var grant in grants)
                {
                    await this.PersistedGrantStore.RemoveAsync(grant.Key);
                }

                // raise the logout event
                await Events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            return View("LoggedOut", vm);
        }

        [HttpGet]
        public IActionResult Registration()
        {
            string nonce = Guid.NewGuid().ToString("N");
            ViewBag.Nonce = nonce;
            return this.View(this.BuildRegistrationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registration1([Bind("ConfirmPassword,Email,FirstName,LastName,Password,CountryCode,PhoneNumber,Address,Postcode,UploadAddressProof,UploadIdProof,AcceptTerms")] RegistrationViewModel model)
        {
            // validate the model
            if (!this.ModelState.IsValid)
            {
                return this.View(this.BuildRegistrationViewModel(model));
            }

            // ensure unique email address
            if (await this.UserManager.FindByEmailAsync(model.Email) != null)
            {
                this.ModelState.AddModelError(string.Empty, "That email address is already registered.");

                return this.View(this.BuildRegistrationViewModel(model));
            }

            // create the new user
            var applicationUser = new ApplicationUser(model.Email);
            applicationUser.IsEnabled = true;
            applicationUser.IsResetPassword = false;
            applicationUser.Postcode = model.Postcode;
            applicationUser.Address = model.Address;
            applicationUser.PhoneNumber = model.PhoneNumber;
            applicationUser.CountryCode = model.CountryCode;
            applicationUser.DateCreated = DateTime.Now;

            var result = await this.UserManager.CreateAsync(applicationUser, model.Password);
            if (result.Succeeded)
            {
                // load the newly created user
                var user = await this.UserManager.FindByEmailAsync(model.Email);

                // add user claims
                var claims = new List<Claim>
                {
                        new Claim(JwtClaimTypes.Name, $"{model.FirstName} {model.LastName}"),
                        new Claim(JwtClaimTypes.GivenName, model.FirstName),
                        new Claim(JwtClaimTypes.FamilyName, model.LastName),
                        new Claim(JwtClaimTypes.Email, model.Email),
                };

                await this.UserManager.AddClaimsAsync(user, claims);
                await this.UserManager.AddToRoleAsync(user, "Landlord");

                Landlord landlord = new Landlord()
                {
                    // Id = user.Id,
                    DateCreated = applicationUser.DateCreated.Value,
                    DateExpiry = applicationUser.DateCreated.Value.AddMonths(2),
                    IsRenew = false
                };
                await this.ApiContext.Landlord.AddAsync(landlord);
                await this.ApiContext.SaveChangesAsync();

                var documentDtos = new List<DocumentDto>();

                if (model.UploadAddressProof != null)
                {
                    documentDtos.Add(new DocumentDto
                    {
                        File = model.UploadAddressProof,
                        OwnerId = landlord.Id,
                        OwnerType = "Landlord",
                        Category = DocumentCategory.AddressProof
                    });
                }

                if (model.UploadIdProof != null)
                {
                    documentDtos.Add(new DocumentDto
                    {
                        File = model.UploadIdProof,
                        OwnerId = landlord.Id,
                        OwnerType = "Landlord",
                        Category = DocumentCategory.IdProof
                    });
                }
                // Call Web API for multiple files
                var uploadedFileUrls = await UploadAllDocumentsAsync(documentDtos);

                // send confirmation email
                var code = await this.UserManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = this.Url.EmailConfirmationLink(user.Id, code, this.Request.Scheme);
                string name = claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Name).Value;
                string link = HtmlEncoder.Default.Encode(callbackUrl);
                string htmlString = $@"
                                            <!DOCTYPE html>
                                            <html>
                                              <head>
                                                <meta charset=""UTF-8"">
                                                <title>Welcome to RentConnect</title>
                                                <style>
                                                  body {{
                                                    font-family: Arial, sans-serif;
                                                    color: #333333;
                                                    line-height: 1.6;
                                                  }}
                                                  .btn {{
                                                    display: inline-block;
                                                    padding: 10px 20px;
                                                    margin-top: 15px;
                                                    font-size: 16px;
                                                    color: #ffffff !important;
                                                    background-color: #007BFF;
                                                    text-decoration: none;
                                                    border-radius: 5px;
                                                  }}
                                                  .footer {{
                                                    margin-top: 30px;
                                                    font-size: 12px;
                                                    color: #888888;
                                                  }}
                                                </style>
                                              </head>
                                              <body>
                                                <p>Dear <strong>{name}</strong>,</p>
                                                <p>Welcome to <strong>RentConnect</strong>! To activate your account, please verify your email address by clicking the button below:</p>
                                                <p><a href=""{link}"" class=""btn"">Verify My Account</a></p>
                                                <p>If the button doesn’t work, copy and paste this link into your browser:</p>
                                                <p><a href=""{link}"">{link}</a></p>
                                                <p>Kind regards,<br>
                                                RentConnect Admin</p>
                                                <div class=""footer"">
                                                  <p>Customer Service Department</p>
                                                  <p>Email: <a href=""mailto:admin@rentconnect.me"">admin@rentconnect.me</a></p>
                                                </div>
                                              </body>
                                            </html>";

                var mailObj = new MailRequestDto()
                {
                    ToEmail = user.Email,
                    Subject = "RentConnect - Registration Confirmation",
                    Body = htmlString,
                    Attachments = null
                };
                await this.Mailer.SendEmailAsync(mailObj);

                return this.RedirectToAction(nameof(RegistrationComplete));
            }

            // if user creation failed, display errors and re-render the view
            foreach (var error in result.Errors)
            {
                this.ModelState.AddModelError(string.Empty, error.Description);
            }

            return this.View(this.BuildRegistrationViewModel(model));
        }

        [HttpGet]
        [Route("[controller]/registration/complete")]
        public IActionResult RegistrationComplete()
        {
            return this.View();
        }

        public IActionResult AccessDenied()
        {
            return this.View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return this.RedirectToAction(nameof(Login));
            }

            var user = await this.UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return this.RedirectToAction(nameof(Login));
            }

            var result = await this.UserManager.ConfirmEmailAsync(user, code);

            return this.View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registration([Bind("ConfirmPassword,Email,FirstName,LastName,Password,CountryCode,PhoneNumber,Address,Postcode,UploadAddressProof,UploadIdProof,AcceptTerms")] RegistrationViewModel model)
        {
            if (!this.ModelState.IsValid)
                return this.View(this.BuildRegistrationViewModel(model));

            if (await this.UserManager.FindByEmailAsync(model.Email) != null)
            {
                this.ModelState.AddModelError(string.Empty, "That email address is already registered.");
                return this.View(this.BuildRegistrationViewModel(model));
            }

            var applicationUser = new ApplicationUser(model.Email)
            {
                IsEnabled = true,
                IsResetPassword = false,
                Postcode = model.Postcode,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                CountryCode = model.CountryCode,
                DateCreated = DateTime.Now
            };

            using var transaction = await this.ApiContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Create user
                var result = await this.UserManager.CreateAsync(applicationUser, model.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        this.ModelState.AddModelError(string.Empty, error.Description);

                    return this.View(this.BuildRegistrationViewModel(model));
                }

                var user = await this.UserManager.FindByEmailAsync(model.Email);

                // 2. Add claims
                var claims = new List<Claim>
                                    {
                                        new Claim(JwtClaimTypes.Name, $"{model.FirstName} {model.LastName}"),
                                        new Claim(JwtClaimTypes.GivenName, model.FirstName),
                                        new Claim(JwtClaimTypes.FamilyName, model.LastName),
                                        new Claim(JwtClaimTypes.Email, model.Email)
                                    };

                await this.UserManager.AddClaimsAsync(user, claims);
                await this.UserManager.AddToRoleAsync(user, "Landlord");

                // 3. Create landlord record
                var landlord = new Landlord
                {
                    DateCreated = applicationUser.DateCreated.Value,
                    DateExpiry = applicationUser.DateCreated.Value.AddMonths(2),
                    IsRenew = false
                };
                await this.ApiContext.Landlord.AddAsync(landlord);
                await this.ApiContext.SaveChangesAsync();

                // 4. Prepare documents
                var documentDtos = new List<DocumentDto>();

                if (model.UploadAddressProof != null)
                    documentDtos.Add(new DocumentDto
                    {
                        File = model.UploadAddressProof,
                        OwnerId = landlord.Id,
                        OwnerType = "Landlord",
                        Category = DocumentCategory.AddressProof
                    });

                if (model.UploadIdProof != null)
                    documentDtos.Add(new DocumentDto
                    {
                        File = model.UploadIdProof,
                        OwnerId = landlord.Id,
                        OwnerType = "Landlord",
                        Category = DocumentCategory.IdProof
                    });

                // 5. Upload documents
                var uploadSuccess = await UploadAllDocumentsAsync(documentDtos);
                if (!uploadSuccess)
                    throw new Exception("Document upload failed.");

                // 6. Send confirmation email
                var code = await this.UserManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = this.Url.EmailConfirmationLink(user.Id, code, this.Request.Scheme);
                string name = claims.First(c => c.Type == JwtClaimTypes.Name).Value;
                string link = HtmlEncoder.Default.Encode(callbackUrl);

                var htmlString = $@"
                        <!DOCTYPE html>
                        <html lang=""en"">
                        <head>
                          <meta charset=""UTF-8"">
                          <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                          <title>Welcome to RentConnect</title>
                        </head>
                        <body style=""margin:0; padding:0; font-family: Arial, Helvetica, sans-serif; background-color:#f4f6f8;"">

                          <table role=""presentation"" style=""width:100%; border-collapse:collapse; border:0; padding:0;"">
                            <tr>
                              <td align=""center"" style=""padding:40px 0;"">

                                <!-- Main Card -->
                                <table role=""presentation"" style=""width:600px; border-collapse:collapse; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 12px rgba(0,0,0,0.1);"">
                                  <!-- Header -->
                                  <tr>
                                    <td style=""background:#2D89EF; padding:20px; text-align:center;"">
                                      <h1 style=""margin:0; font-size:24px; color:#ffffff; font-weight:bold;"">RentConnect</h1>
                                    </td>
                                  </tr>

                                  <!-- Body -->
                                  <tr>
                                    <td style=""padding:30px; color:#333333; font-size:16px; line-height:1.6;"">
                                      <p>Dear <strong>{name}</strong>,</p>
                                      <p>Welcome to <strong>RentConnect</strong>! We’re excited to have you on board.
                                         Please confirm your email address to activate your account.</p>

                                      <p style=""text-align:center; margin:30px 0;"">
                                        <a href=""{link}"" style=""display:inline-block; padding:14px 28px; background:#2D89EF; color:#ffffff; text-decoration:none; font-size:16px; border-radius:6px; font-weight:bold;"">Verify Account</a>
                                      </p>

                                      <p>If the button above doesn’t work, copy and paste this link into your browser:</p>
                                      <p style=""word-break:break-all; color:#2D89EF;""><a href=""{link}"" style=""color:#2D89EF; text-decoration:none;"">{link}</a></p>

                                      <p>Kind regards,</p>
                                      <p><strong>RentConnect Admin</strong></p>
                                    </td>
                                  </tr>

                                  <!-- Footer -->
                                  <tr>
                                    <td style=""background:#f0f2f5; text-align:center; padding:15px; font-size:12px; color:#666;"">
                                      &copy; {DateTime.Now.Year} RentConnect. All rights reserved.
                                    </td>
                                  </tr>
                                </table>

                              </td>
                            </tr>
                          </table>

                        </body>
                        </html>";

                var mailObj = new MailRequestDto
                {
                    ToEmail = user.Email,
                    Subject = "RentConnect - Registration Confirmation",
                    Body = htmlString
                };
                await this.Mailer.SendEmailAsync(mailObj);

                // 7. Commit transaction
                await transaction.CommitAsync();

                return this.RedirectToAction(nameof(RegistrationComplete));
            }
            catch (Exception ex)
            {
                // Rollback transaction
                await transaction.RollbackAsync();

                // Delete the user if created
                var existingUser = await this.UserManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                    await this.UserManager.DeleteAsync(existingUser);

                // Log exception if you have logger
                this.ModelState.AddModelError(string.Empty, $"Registration failed: {ex.Message}");
                return this.View(this.BuildRegistrationViewModel(model));
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    ViewBag.errorMessage = new HtmlString("<p>An email has been sent to your registered email.</p> Please check your email and follow the instructions. <p>Please check your Spam folder for the email if you've not recieved. </p>");
                    var user = await this.UserManager.FindByEmailAsync(model.Email);

                    if (user == null)
                    { // reveal that the user does not exist or is not confirmed
                        ViewBag.errorMessage = "User Not Found";
                        return View("ForgotPasswordConfirmation");
                    }

                    if (!(await this.UserManager.IsEmailConfirmedAsync(user)))
                    { // reveal that the user does not exist or is not confirmed
                        ViewBag.errorMessage = new HtmlString($"<p>Your email address has not been confirmed yet. Please check your email and follow the instructions to confirm your email address.</p><p>If you haven't received the confirmation email, you can request a new one by clicking <a href=\"{Url.Action("EmailConfirmationRequired")}\">here</a>.</p>");

                        return View("ForgotPasswordConfirmation");
                    }

                    var code = await UserManager.GeneratePasswordResetTokenAsync(user);
                    var callbackUrl = this.Url.ResetPasswordCallbackLink(user.Id, code, this.Request.Scheme);
                    // Get the name and link from the claims and callbackUrl, respectively
                    // Get the current user's claims
                    var claims = await UserManager.GetClaimsAsync(user);
                    string name = claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value;
                    string link = HtmlEncoder.Default.Encode(callbackUrl);

                    // Load the HTML template
                    string htmlTemplate = $@"
                                        <!DOCTYPE html>
                                        <html>
                                        <head>
                                          <meta charset='UTF-8'>
                                          <title>RentConnect - Reset Password</title>
                                          <style>
                                            body {{
                                              font-family: Arial, Helvetica, sans-serif;
                                              background-color: #f4f6f8;
                                              margin: 0;
                                              padding: 0;
                                            }}
                                            .container {{
                                              max-width: 600px;
                                              margin: 30px auto;
                                              background: #ffffff;
                                              border-radius: 12px;
                                              overflow: hidden;
                                              box-shadow: 0 4px 12px rgba(0,0,0,0.1);
                                            }}
                                            .header {{
                                              background-color: #2c3e50;
                                              padding: 20px;
                                              text-align: center;
                                              color: #ffffff;
                                            }}
                                            .header h1 {{
                                              margin: 0;
                                              font-size: 22px;
                                            }}
                                            .content {{
                                              padding: 30px;
                                              color: #333333;
                                              font-size: 15px;
                                              line-height: 1.6;
                                            }}
                                            .button {{
                                              display: inline-block;
                                              padding: 12px 24px;
                                              margin: 20px 0;
                                              font-size: 16px;
                                              color: #ffffff;
                                              background-color: #3498db;
                                              border-radius: 6px;
                                              text-decoration: none;
                                              font-weight: bold;
                                            }}
                                            .footer {{
                                              background-color: #f4f6f8;
                                              padding: 15px;
                                              text-align: center;
                                              font-size: 12px;
                                              color: #777777;
                                            }}
                                          </style>
                                        </head>
                                        <body>
                                          <div class='container'>
                                            <div class='header'>
                                              <h1>RentConnect Account Security</h1>
                                            </div>
                                            <div class='content'>
                                              <p>Dear <strong>{name}</strong>,</p>
                                              <p>We have received a request to reset your password for your RentConnect account.
                                              If you did not make this request, you can ignore this email and your account will remain secure.</p>
                                              <p>To reset your password, please click the button below:</p>
                                              <p style='text-align:center;'>
                                                <a href='{link}' class='button'>Reset Password</a>
                                              </p>
                                              <p>If the button does not work, copy and paste the link below into your browser:</p>
                                              <p><a href='{link}'>{link}</a></p>
                                              <p>Kind regards,<br><strong>RentConnect Admin</strong><br>
                                              Customer Service Department</p>
                                              <p><strong>Email:</strong> admin@RentConnect.me</p>
                                            </div>
                                            <div class='footer'>
                                              © 2025 RentConnect. All rights reserved.
                                            </div>
                                          </div>
                                        </body>
                                        </html>";

                    var mailObj = new MailRequestDto()
                    {
                        ToEmail = user.Email,
                        Subject = "RentConnect - Rest Password",
                        Body = htmlTemplate,
                        Attachments = null
                    };
                    await this.Mailer.SendEmailAsync(mailObj);
                    return View("ForgotPasswordConfirmation");
                }

                // If we got this far, something failed, redisplay form
                return View(model);
            }
            catch (Exception ex) { throw ex; }
        }

        public IActionResult Lockout()
        {
            return this.View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordTenant(string email = null)
        {
            if (email == null)
            {
                throw new ApplicationException("A email must be supplied for password reset.");
            }
            var user = await this.UserManager.FindByEmailAsync(email);
            var code = await UserManager.GeneratePasswordResetTokenAsync(user);

            var model = new ResetPasswordViewModel { Code = code, Email = email };
            return this.RedirectToAction(nameof(this.ResetPassword), new { code });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }

            ResetPasswordViewModel model = new ResetPasswordViewModel { Code = code };
            return this.View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            var user = await this.UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // don't reveal that the user does not exist
                return this.RedirectToAction(nameof(this.ResetPasswordConfirmation));
            }

            var result = await this.UserManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                if (user.LockoutEnabled)
                {
                    user.LockoutEnabled = false;
                    await this.UserManager.UpdateAsync(user);
                }

                return this.RedirectToAction(nameof(this.ResetPasswordConfirmation));
            }

            // on failure, add errors to model
            foreach (var error in result.Errors)
            {
                this.ModelState.AddModelError(string.Empty, error.Description);
            }

            return this.View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return this.View();
        }

        /// <summary>
        /// Constructs logged out view model.
        /// </summary>
        /// <param name="logoutId"></param>
        /// <returns></returns>
        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for
            // federated signout)
            var logout = await Interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider)
                {
                    // New way: resolve the scheme from the authentication scheme provider
                    var schemeProvider = HttpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                    var scheme = await schemeProvider.GetSchemeAsync(idp);
                    if (scheme != null)
                    {
                        var handlerProvider = HttpContext.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
                        var handler = await handlerProvider.GetHandlerAsync(HttpContext, idp);

                        if (handler is IAuthenticationSignOutHandler)
                        {
                            // The external provider supports signout
                            if (vm.LogoutId == null)
                            {
                                // If there's no current logout context, create one
                                vm.LogoutId = await Interaction.CreateLogoutContextAsync();
                            }
                        }
                    }
                }
            }

            return vm;
        }

        /// <summary>
        /// Constructs view model for login view.
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await Interaction.GetAuthorizationContextAsync(returnUrl);
            var schemes = await SchemeProvider.GetAllSchemesAsync();

            return new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
            };
        }

        /// <summary>
        /// Constructs view model for login view based including previous user input.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.Username = model.Username;
            vm.RememberLogin = model.RememberLogin;

            return vm;
        }

        /// <summary>
        /// Constructs logout view model.
        /// </summary>
        /// <param name="logoutId"></param>
        /// <returns></returns>
        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await Interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user is automatically signed
            // out by another malicious web page.
            return vm;
        }

        /// <summary>
        /// Builds registration view model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private RegistrationViewModel BuildRegistrationViewModel(RegistrationViewModel model = null)
        {
            // set the model
            model = model ?? new RegistrationViewModel();

            return model;
        }

        private bool RenewStatus(Landlord landlord)
        {
            if (!landlord.IsRenew.Value) { return true; }

            TimeSpan diffDays = landlord.DateExpiry.Value.Date - DateTime.Now.Date;
            if ((diffDays.TotalDays + 1) <= 15) { return true; }
            return false;
        }

        private async Task<bool> UploadAllDocumentsAsync(List<DocumentDto> documents)
        {
            if (documents == null || !documents.Any())
                return false;

            using var httpClient = new HttpClient();
            using var content = new MultipartFormDataContent();

            var streams = new List<Stream>(); // Keep track of opened streams

            try
            {
                for (int i = 0; i < documents.Count; i++)
                {
                    var doc = documents[i];
                    if (doc.File == null || doc.File.Length == 0)
                        continue;

                    // Do NOT dispose here, keep it alive until request completes
                    var fileStream = doc.File.OpenReadStream();
                    streams.Add(fileStream);

                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(doc.File.ContentType);

                    // Add file and metadata
                    content.Add(fileContent, $"Documents[{i}].File", doc.File.FileName);
                    content.Add(new StringContent(doc.OwnerId.ToString()), $"Documents[{i}].OwnerId");
                    content.Add(new StringContent(doc.OwnerType), $"Documents[{i}].OwnerType");
                    content.Add(new StringContent(((int)doc.Category).ToString()), $"Documents[{i}].DocumentType");
                }

                var apiUrl = "http://localhost:6001/api/Document/upload";
                var response = await httpClient.PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    // log errorMsg
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                // log exception
                return false;
            }
            finally
            {
                // Dispose streams AFTER request completes
                foreach (var stream in streams)
                    stream.Dispose();
            }
        }
    }
}