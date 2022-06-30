using CsvHelper.Configuration.Attributes;
using Newtonsoft.Json;

namespace AzureADB2C.Invite.Models
{
	public class Invite
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("message")]
		[Ignore]
		public string Message { get; set; }
	}
}
