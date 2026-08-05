using Kidamooz.Domain.Entities;

namespace Kidamooz.Infrastructure.Cover;

public class StoryCoverPromptGenerator : ICoverPromptGenerator
{
    private const int MaxLength = 400;
    private const string StyleSuffix =
        "children's book illustration, watercolor, digital painting, colorful, soft pastel colors, magical, cute, highly detailed, storybook art, warm lighting, Pixar inspired, Disney inspired, smiling characters, fantasy, no text, no logo, no watermark";

    public string GeneratePrompt(StoryDraft draft)
    {
        var subject = !string.IsNullOrWhiteSpace(draft.CoverPrompt)
            ? draft.CoverPrompt.Trim()
            : BuildSubjectFromDraft(draft);

        var prompt = $"{subject.TrimEnd('.')}. {StyleSuffix}";
        if (prompt.Length <= MaxLength)
            return prompt;

        var subjectBudget = MaxLength - StyleSuffix.Length - 2;
        if (subjectBudget < 32)
            return prompt[..MaxLength];

        var trimmedSubject = subject.Length <= subjectBudget
            ? subject
            : subject[..subjectBudget].TrimEnd() + "…";

        return $"{trimmedSubject}. {StyleSuffix}";
    }

    private static string BuildSubjectFromDraft(StoryDraft draft)
    {
        var title = string.IsNullOrWhiteSpace(draft.TitleFa) ? "a children's story" : draft.TitleFa.Trim();
        var description = string.IsNullOrWhiteSpace(draft.DescriptionFa)
            ? string.Empty
            : $" {draft.DescriptionFa.Trim()}";
        return $"A joyful children's story about {title}.{description}";
    }
}
