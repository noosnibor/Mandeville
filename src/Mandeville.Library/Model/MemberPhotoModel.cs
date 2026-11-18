namespace Mandeville.Library.Model;

/// <summary>A member's saved photo (already shrunk by the browser to a small square JPEG).</summary>
public class MemberPhotoModel
{
    public Guid flngMemberKey { get; set; }
    public byte[] fbinPhoto { get; set; } = [];
    public string fstrContentType { get; set; } = "image/jpeg";

    /// <summary>The photo as a data address a web page can show straight away.</summary>
    public string DataUrl => $"data:{fstrContentType};base64,{Convert.ToBase64String(fbinPhoto)}";
}
