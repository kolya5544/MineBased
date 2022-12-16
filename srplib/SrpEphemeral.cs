namespace SecureRemotePassword
{
	/// <summary>
	/// Ephemeral values used during the SRP-6a authentication.
	/// </summary>
	public class SrpEphemeral
	{
		/// <summary>
		/// Gets or sets the public part.
		/// </summary>
		public byte[] Public { get; set; }

		/// <summary>
		/// Gets or sets the secret part.
		/// </summary>
		public byte[] Secret { get; set; }
	}
}
