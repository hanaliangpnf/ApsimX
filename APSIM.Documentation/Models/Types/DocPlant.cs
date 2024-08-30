using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using Svg;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocPlant : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPlant" /> class.
        /// </summary>
        public DocPlant(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();
            
            List<ITag> subTags = new List<ITag>();

            subTags.Add(new Paragraph($"The model is constructed from the following list of software components. Details of the implementation and model parameterisation are provided in the following sections."));

            // Write Plant Model Table
            subTags.Add(new Paragraph("**List of Plant Model Components.**"));
            DataTable tableData = new DataTable();
            tableData.Columns.Add("Component Name", typeof(string));
            tableData.Columns.Add("Component Type", typeof(string));
            foreach (IModel child in this.model.Children)
            {
                if (child.GetType() != typeof(Memo) && child.GetType() != typeof(Cultivar) && child.GetType() != typeof(Folder) && child.GetType() != typeof(CompositeBiomass))
                {
                    DataRow row = tableData.NewRow();
                    row[0] = child.Name;
                    row[1] = child.GetType().ToString();
                    tableData.Rows.Add(row);
                }
            }
            subTags.Add(new Table(tableData));

            List<Type> documentableModels = new()
            {
                typeof(IOrgan), 
                typeof(IPhenology), 
                typeof(IArbitrator),
                typeof(IBiomass),
            };

            Memo introduction = this.model.Children?.FirstOrDefault() as Memo;
            // Document children.
            foreach (IModel child in this.model.Children)
            {
                if (child != introduction)
                {
                    foreach (Type type in documentableModels)
                        if (type.IsAssignableFrom(child.GetType()))
                        {
                            if (child is Phenology)
                                subTags.AddRange(AutoDocumentation.Document(child, subTags, headingLevel+1, indent+1));
                            else
                            {
                                ITag firstChildTag = child.Document()?.First();
                                subTags.Add(new Section(new List<ITag> { firstChildTag }));
                            }
                        }
                }
                if (child is Folder && child.Name == "Cultivars")
                {
                    DataTable cultivarNameTable = new();
                    cultivarNameTable.Columns.Add("Cultivar Name");
                    cultivarNameTable.Columns.Add("Alternative Name(s)");
                   
                    foreach (Folder folder in child.FindAllChildren<Folder>())
                    {
                        List<Cultivar> cultivars = folder.FindAllChildren<Cultivar>().ToList();
                        foreach (Cultivar cultivarChild in cultivars)
                        {
                            string altNames = cultivarChild.GetNames().Any() ? string.Join(',', cultivarChild.GetNames()) : string.Empty;
                            cultivarNameTable.Rows.Add(new string[] { cultivarChild.Name, altNames});
                        }
                    }
                    subTags.Add(new Section("Cultivars", new Table(cultivarNameTable)));
                }
            }

            newTags.Add(new Section($"The APSIM {model.Name} Model", subTags));
            return newTags;
        }
    }
}
