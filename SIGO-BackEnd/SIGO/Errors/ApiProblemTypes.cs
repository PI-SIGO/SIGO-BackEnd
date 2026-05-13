namespace SIGO.Errors
{
    public static class ApiProblemTypes
    {
        public const string BadRequest = "urn:sigo:problem:bad-request";
        public const string Validation = "urn:sigo:problem:validation";
        public const string Unauthorized = "urn:sigo:problem:unauthorized";
        public const string Forbidden = "urn:sigo:problem:forbidden";
        public const string NotFound = "urn:sigo:problem:not-found";
        public const string Conflict = "urn:sigo:problem:conflict";
        public const string RateLimit = "urn:sigo:problem:rate-limit";
        public const string InternalServerError = "urn:sigo:problem:internal-server-error";
        public const string HttpError = "urn:sigo:problem:http-error";
    }
}
