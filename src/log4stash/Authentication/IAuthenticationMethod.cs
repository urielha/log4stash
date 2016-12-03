namespace log4stash.Authentication
{
    public interface IAuthenticationMethod
    {
        /// <summary>
        /// Creates a string for the authentication header, representing the credentials requested
        /// </summary>
        /// <param name="requestData">Request data relevant for the header</param>
        /// <returns>a string containing the new authentication header</returns>
        string CreateAuthenticationHeader(RequestData requestData);
    }
}
