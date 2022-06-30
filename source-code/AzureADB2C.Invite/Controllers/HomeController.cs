using AzureADB2C.Invite.Models;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace AzureADB2C.Invite.Controllers
{


	public class HomeController : Controller
	{
		private static Lazy<X509SigningCredentials> SigningCredentials;
		private readonly AppSettingsModel AppSettings;
		private readonly IWebHostEnvironment HostingEnvironment;
		private readonly ILogger<HomeController> _logger;

		// Sample: Inject an instance of an AppSettingsModel class into the constructor of the consuming class, 
		// and let dependency injection handle the rest
		public HomeController(ILogger<HomeController> logger, IOptions<AppSettingsModel> appSettings, IWebHostEnvironment hostingEnvironment)
		{
			this.AppSettings = appSettings.Value;
			this.HostingEnvironment = hostingEnvironment;
			this._logger = logger;

			// Sample: Load the certificate with a private key (must be pfx file)
			SigningCredentials = new Lazy<X509SigningCredentials>(() =>
			{

				X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
				certStore.Open(OpenFlags.ReadOnly);
				X509Certificate2Collection certCollection = certStore.Certificates.Find(
											X509FindType.FindByThumbprint,
											this.AppSettings.SigningCertThumbprint,
											false);
				// Get the first cert with the thumb-print
				if (certCollection.Count > 0)
				{
					return new X509SigningCredentials(certCollection[0]);
				}

				throw new Exception("Certificate not found");
			});
		}

		[Authorize]
		[HttpPost]
		public IActionResult Index(IFormFile postedFile)
		{
			try
			{
				if (postedFile != null)
				{
					using var reader = new StreamReader(postedFile.OpenReadStream());
					using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
					var records = csv.GetRecords<Models.Invite>().ToList();
					if (records.Count > 0)
					{
						foreach (var inv in records)
						{
							inv.Message = SendEmail(inv.Name, inv.Email, null);
						}
					}
					return View(records);
				}
			}
			catch (Exception)
			{
				throw;
			}

			return View();
		}

		[Authorize]
		[HttpGet]
		public ActionResult Index(string Name, string email, string phone)
		{

			ViewData["Message"] = SendEmail(Name, email, phone);
			return View();
		}

		private string SendEmail(string name, string email, string phone)
		{

			if (string.IsNullOrEmpty(email))
			{
				return "no email specified";
			}

			try
			{
				string token = BuildIdToken(name, email, phone);
				string link = BuildUrl(token);

				string Body = string.Empty;

				string htmlTemplate = System.IO.File.ReadAllText(Path.Combine(this.HostingEnvironment.ContentRootPath, "App_Data\\Template.html"));



				//MailMessage mailMessage = new MailMessage();
				//mailMessage.To.Add(email);
				//mailMessage.From = new MailAddress(AppSettings.SMTPFromAddress);
				//mailMessage.Subject = AppSettings.SMTPSubject;
				//mailMessage.Body = string.Format(htmlTemplate, email, link);
				//mailMessage.IsBodyHtml = true;
				//SmtpClient smtpClient = new SmtpClient(AppSettings.SMTPServer, AppSettings.SMTPPort);
				//smtpClient.Credentials = new NetworkCredential(AppSettings.SMTPUsername, AppSettings.SMTPPassword);
				//smtpClient.EnableSsl = AppSettings.SMTPUseSSL;
				//smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
				//smtpClient.Send(mailMessage);

				return $"Email sent to {email} {link}";

			}
			catch (Exception ex)
			{
				return $"Email failed to send {email} {ex.Message}";
			}
		}


		private string BuildIdToken(string name, string email, string phone)
		{
			string issuer = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase.Value}/";

			// All parameters send to Azure AD B2C needs to be sent as claims
			IList<System.Security.Claims.Claim> claims = new List<System.Security.Claims.Claim>();
			claims.Add(new System.Security.Claims.Claim("name", name, System.Security.Claims.ClaimValueTypes.String, issuer));
			claims.Add(new System.Security.Claims.Claim("displayName", name, System.Security.Claims.ClaimValueTypes.String, issuer));
			claims.Add(new System.Security.Claims.Claim("email", email, System.Security.Claims.ClaimValueTypes.String, issuer));

			if (!string.IsNullOrEmpty(phone))
			{
				claims.Add(new System.Security.Claims.Claim("phone", phone, System.Security.Claims.ClaimValueTypes.String, issuer));
			}

			// Create the token
			JwtSecurityToken token = new JwtSecurityToken(
					issuer,
					this.AppSettings.B2CClientId,
					claims,
					DateTime.Now,
					DateTime.Now.AddDays(7),
					HomeController.SigningCredentials.Value);

			// Get the representation of the signed token
			JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();

			return jwtHandler.WriteToken(token);
		}

		private string BuildUrl(string token)
		{
			string nonce = Guid.NewGuid().ToString("n");

			return string.Format(this.AppSettings.B2CSignUpUrl,
					this.AppSettings.B2CTenant,
					this.AppSettings.B2CPolicy,
					this.AppSettings.B2CClientId,
					Uri.EscapeDataString(this.AppSettings.B2CRedirectUri),
					nonce) + "&id_token_hint=" + token;
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
