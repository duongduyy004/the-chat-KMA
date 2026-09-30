using System.Text;

public static class VietText
{
    public static string Fix(string s) =>
        string.IsNullOrEmpty(s) ? s : s.Normalize(NormalizationForm.FormC);
}
