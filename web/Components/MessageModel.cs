using System.ComponentModel.DataAnnotations;

namespace N8.Web.Components;

internal class MessageModel
{
    [Required]
    public string Message { get; set; }
}
