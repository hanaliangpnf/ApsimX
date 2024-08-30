using APSIM.Shared.Documentation;
using Models.Core;
using System.Collections.Generic;
using System.Linq;
using Models.PMF.Struct;
using Models.Functions;
using Models.PMF;

namespace APSIM.Documentation.Models.Types;

/// <summary>
/// Base documentation class for models
/// </summary>
public class DocStructure : DocGeneric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Structure"/> class.
    /// </summary>
    public DocStructure(IModel model) : base(model) { }

    /// <summary>
    /// Document the model.
    /// </summary>
    public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
    {
        List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();

        newTags.Add(new Section("Plant and Main-Stem Population", new Paragraph(
                $"The *Plant.Population* is set at sowing with information sent from a manager script in the Sow method. " +
                $"The *PrimaryBudNumber* is also sent with the Sow method. The main-stem population (*MainStemPopn*) is calculated as:\n\n" +
                $"*MainStemPopn* = *Plant.Population* x *PrimaryBudNumber*\n\n" +
                $"Primary bud number is > 1 for crops like potato and grape vine where there are more than one main-stem per plant"
                )));

        var leafTags = new List<ITag>()
        {
            new Paragraph("Each day the number of main-stem leaf tips appeared (*LeafTipsAppeared*) is calculated as:"),
            new Paragraph("*LeafTipsAppeared* += *DeltaTips*"),
            new Paragraph("Where *DeltaTips* is calculated as:"),
            new Paragraph("*DeltaTips* = *ThermalTime* / *Phyllochron*"),
        };

        if (model.FindChild<IFunction>("phyllochron").Document() is List<ITag> phyllochronTags)
        {
            leafTags.Add(new Paragraph("Where *Phyllochron* is the thermal time duration between the appearance of leaf tips given by:"));
            leafTags.AddRange(phyllochronTags);
        }

        if (model.FindChild<IFunction>("thermalTime").Document() is List<ITag> thermalTimeTags)
        {
            leafTags.Add(new Paragraph("*ThermalTime* is given by"));
            leafTags.AddRange(thermalTimeTags);
        }

        if (model.FindChild<IFunction>("finalLeafNumber").Document() is List<ITag> finalLeafNumberTags)
        {
            leafTags.Add(new Paragraph("*LeafTipsAppeared* continues to increase until *FinalLeafNumber* is reached where *FinalLeafNumber* is calculated as:"));
            leafTags.AddRange(finalLeafNumberTags);
        }

        newTags.Add(new Section("Main-Stem leaf appearance", leafTags));

        var branchingTags = new List<ITag>()
        {
            new Paragraph("The total population of stems (*TotalStemPopn*) is calculated as:"),
            new Paragraph("*TotalStemPopn* = *MainStemPopn* + *NewBranches* - *NewlyDeadBranches*"),
            new Paragraph("Where:"),
            new Paragraph("*NewBranches* = *MainStemPopn* x *BranchingRate*"),
            new Paragraph("*BranchingRate* is given by:"),
        };

        if (model.FindChild<IFunction>("branchingRate").Document() is List<ITag> branchingRateTags)
        {
            branchingTags.AddRange(branchingRateTags);
        }
        
        branchingTags.Add(new Paragraph("*NewlyDeadBranches* is calcualted as:"));
        branchingTags.Add(new Paragraph("*NewlyDeadBranches* = (*TotalStemPopn* - *MainStemPopn*) x *BranchMortality*"));
        branchingTags.Add(new Paragraph("where *BranchMortality* is given by:"));

        if (model.FindChild<IFunction>("branchMortality").Document() is List<ITag> branchMortalityTags)
        {
            branchingTags.AddRange(branchMortalityTags);
        }

        newTags.Add(new Section("Branching and Branch Mortality", branchingTags));

        var heightTags = new List<ITag>
        {
            new Paragraph("The height of the crop is calculated by the *HeightModel*")
        };

        if (model.FindChild<IFunction>("heightModel").Document() is List<ITag> heightModelTags)
        {
            heightTags.AddRange(heightModelTags);
        }
        newTags.Add(new Section("Height", heightTags));

        return newTags;
    }
}
