using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using RestSharp;

namespace log4stash.ElasticClient
{
    public class ResponseValidator : IResponseValidator
    {
        public void ValidateResponse(IRestResponse response)
        {
            CheckResponse(response);
        }

        private static void CheckResponse(IRestResponse response)
        {
            var errString = GetResponseErrorIfAny(response);
            if (string.IsNullOrEmpty(errString))
            {
                return;
            }

            throw new InvalidOperationException(
                string.Format("Some error occurred while sending request to ElasticSearch.{0}{1}",
                    Environment.NewLine, errString));
        }

        private static string GetResponseErrorIfAny(IRestResponse response)
        {
            if (response == null)
            {
                return "Got null response";
            }

            // Handle network transport or framework exception
            if (response.ErrorException != null)
            {
                return response.ErrorException.ToString();
            }

            // Handle request errors
            if (!response.StatusCode.HasFlag(HttpStatusCode.OK))
            {
                var err = new StringBuilder();
                err.AppendFormat("Got non ok status code: {0}.", response.StatusCode);
                err.AppendLine(response.Content);
                return err.ToString();
            }

            // Handle index error
            try
            {
                var jsonResponse = JsonConvert.DeserializeObject<PartialElasticResponse>(response.Content);
                if (jsonResponse != null && jsonResponse.Errors)
                {
                    return response.Content;
                }
            }
            catch (JsonReaderException)
            {
                return string.Format("Can't parse Elastic response: {0}", response.Content);
            }

            return null;
        }
        
    }
}
