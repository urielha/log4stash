namespace log4stash.Authentication
{
    public class AuthenticationMethodChooser : IAuthenticationMethod
    {
        private IAuthenticationMethod _innerMethod = null;

        public string CreateAuthenticationHeader(RequestData requestData)
        {
            if (_innerMethod == null)
            {
                return string.Empty;
            }
            return _innerMethod.CreateAuthenticationHeader(requestData);
        }

        public void AddFilter(IAuthenticationMethod method)
        {
            _innerMethod = method;
        }

        #region Helpers for common authentication methods

        public void AddBasic(BasicAuthenticationMethod method)
        {
            AddFilter(method);
        }

        public void AddAws(AwsAuthenticationMethod method)
        {
            AddFilter(method);
        }

        #endregion
    }
}
