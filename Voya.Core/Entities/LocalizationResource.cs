namespace Voya.Core.Entities;

public class LocalizationResource
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Key { get; set; } = string.Empty; // "Button_AddToCart"
	public string LanguageCode { get; set; } = "en"; // "en", "ar"
	public string Value { get; set; } = string.Empty; // "Add to Cart", "أضف للسلة"
}