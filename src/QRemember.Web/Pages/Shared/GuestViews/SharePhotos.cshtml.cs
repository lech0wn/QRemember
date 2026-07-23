using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QRemember.Web.Pages
{
    public class SharePhotosModel : PageModel
    {
        public int MaxPhotos { get; } = 15;

        [BindProperty(SupportsGet = true)]
        public string EventCode { get; set; } = string.Empty;

        public string EventHashtag { get; set; } = string.Empty;

        public void OnGet()
        {
            // TODO: replace with a real lookup once you wire up the backend, e.g.:
            // var eventEntity = _db.Events.FirstOrDefault(e => e.Code == EventCode && e.IsActive);
            // EventHashtag = eventEntity.Hashtag;
            EventHashtag = EventCode;
        }
    }
}
