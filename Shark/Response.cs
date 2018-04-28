using System;

namespace Shark
{
    public class Response
    {
        public Response()
        {
        }

        public static implicit operator Response(String str)
        {
            Response response = new Response()
            {
                Body = str,
                ResponseCode = 200
            };

            return response;
        }

        public String Body { get; set; }
        public int ResponseCode { get; set; }
    }
}
