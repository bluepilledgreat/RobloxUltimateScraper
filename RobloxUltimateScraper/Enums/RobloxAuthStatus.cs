namespace RobloxUltimateScraper.Enums
{
    internal enum RobloxAuthStatus
    {
        /// <summary>
        /// The auth cookie being used is valid
        /// </summary>
        Authenticated,

        /// <summary>
        /// No authentication cookie is set 
        /// </summary>
        Unauthenticated,

        /// <summary>
        /// The auth cookie being used is invalid
        /// </summary>
        InvalidAuth,

        /// <summary>
        /// An error occured while validating
        /// </summary>
        Error
    }
}
