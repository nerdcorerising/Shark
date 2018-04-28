using System;

namespace Shark
{
    public class Response
    {
        // Informational
        public static int Continue = 100;

        // Success
        public static int OK = 200;
        public static int Created = 201;
        public static int Accepted = 202;
        public static int NoContent = 204;
        public static int ResetContent = 205;
        public static int PartialContent = 206;

        // Redirection
        public static int MultipleChoices = 300;
        public static int Moved = 301;
        public static int Found = 302;
        public static int SeeOther = 303;
        public static int NotModified = 304;
        public static int UseProxy = 305;
        public static int TemporaryRedirect = 307;

        // Errors
        public static int BadRequest = 400;
        public static int Unauthorized = 401;
        public static int PaymentRequired = 402;
        public static int Forbidden = 403;
        public static int NotFound = 404;
        public static int NotAllowed = 405;
        public static int NotAcceptable = 406;
        public static int ProxyAuthenticationRequired = 407;
        public static int RequestTimeout = 408;
        public static int Conflict = 409;
        public static int Gone = 410;
        public static int LengthRequired = 411;
        public static int PreconditionFailed = 412;
        public static int EntityTooLarge = 413;
        public static int URITooLong = 414;
        public static int UnsupportedMedia = 415;
        public static int RangeNotSatisfiable = 416;
        public static int ExpectationFailed = 417;

        public static int InternalServerError = 500;
        public static int BadGateway = 502;
        public static int GatewayTimeout = 504;
        public static int HttpVersionNotSupported = 505;

        public Response()
        {
        }

        public static implicit operator Response(String str)
        {
            Response response = new Response()
            {
                Body = str,
                ResponseCode = Response.OK
            };

            return response;
        }

        public String Body { get; set; }
        public int ResponseCode { get; set; }

        public static Response Error(string body, int code)
        {
            Response response = new Response()
            {
                Body = body,
                ResponseCode = code
            };

            return response;
        }
    }
}
