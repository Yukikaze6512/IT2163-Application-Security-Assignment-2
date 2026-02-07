using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApplicationSecurity.Pages
{
    public class StatusCodeModel : PageModel
    {
        public int Code { get; set; }
        public string Title { get; set; } = "Error";
        public string Description { get; set; } = "An unexpected error occurred.";

        public void OnGet(int code)
        {
            Code = code;
            (Title, Description) = code switch
            {
                400 => ("Bad Request", "The server could not understand the request."),
                401 => ("Unauthorized", "You need to log in to access this resource."),
                403 => ("Access Denied", "You do not have permission to access this resource."),
                404 => ("Page Not Found", "The page you are looking for does not exist or has been moved."),
                405 => ("Method Not Allowed", "The request method is not supported for this resource."),
                408 => ("Request Timeout", "The server timed out waiting for the request."),
                500 => ("Internal Server Error", "Something went wrong on our end. Please try again later."),
                502 => ("Bad Gateway", "The server received an invalid response from an upstream server."),
                503 => ("Service Unavailable", "The server is temporarily unavailable. Please try again later."),
                _ => ("Error", "An unexpected error occurred.")
            };
        }
    }
}
