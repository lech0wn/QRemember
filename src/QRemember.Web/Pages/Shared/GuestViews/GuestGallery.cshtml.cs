using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QRemember.Web.Pages
{
    public class GuestGalleryModel : PageModel
    {
        // TODO: inject your DbContext / photo storage service here, e.g.:
        // private readonly ApplicationDbContext _db;
        // public GuestGalleryModel(ApplicationDbContext db) => _db = db;

        [BindProperty(SupportsGet = true)]
        public string EventCode { get; set; } = string.Empty;

        public string EventHashtag { get; set; } = string.Empty;

        public List<SubmissionPhoto> Photos { get; set; } = new();

        public void OnGet()
        {
            // TODO: replace with a real lookup, e.g.:
            // var eventEntity = _db.Events.FirstOrDefault(e => e.Code == EventCode && e.IsActive);
            // EventHashtag = eventEntity.Hashtag;
            // Photos = _db.EventPhotos
            //     .Where(p => p.EventCode == EventCode)
            //     .OrderByDescending(p => p.UploadedAt)
            //     .Select(p => new SubmissionPhoto { Url = p.Url, GuestName = p.GuestName })
            //     .ToList();

            EventHashtag = EventCode;
        }

        public class SubmissionPhoto
        {
            public string Url { get; set; } = string.Empty;
            public string? GuestName { get; set; }
        }
    }
}
