using Kidamooz.Domain.Entities;

namespace Kidamooz.Infrastructure.Cover;

public interface ICoverPromptGenerator
{
    string GeneratePrompt(StoryDraft draft);
}
