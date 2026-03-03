using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models;

public class SupportingDocumentsViewModel
{
    public DocumentEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public IEnumerable<SupportingDocument> Documents { get; set; } = Enumerable.Empty<SupportingDocument>();

    /// <summary>URL to redirect back to after upload or delete (full-page fallback).</summary>
    public string ReturnUrl { get; set; } = "/";

    /// <summary>
    /// URL to reload the parent partial via AJAX after a successful upload or delete.
    /// When set, operations use AJAX and reload the offcanvas content in-place.
    /// </summary>
    public string? ReloadUrl { get; set; }
}
