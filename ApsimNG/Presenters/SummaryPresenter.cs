﻿namespace UserInterface.Presenters
{
    using EventArguments;
    using System;
    using System.IO;
    using System.Linq;
    using Models;
    using Models.Core;
    using Models.Factorial;
    using Views;
    using Commands;
    using Utility;
    using Models.Storage;
    using System.Collections.Generic;
    using Models.Core.Run;
    using Models.Logging;
    using ErrorLevel = Models.Core.MessageType;
    using System.Text;

    /// <summary>Presenter class for working with a summary component</summary>
    public class SummaryPresenter : IPresenter
    {
        /// <summary>The summary model to work with.</summary>
        private Summary summaryModel;

        /// <summary>The view model to work with.</summary>
        private ISummaryView summaryView;

        /// <summary>The explorer presenter which manages this presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// This dictionary maps simulation names to lists of messages.
        /// </summary>
        private Dictionary<string, IEnumerable<Message>> messages = new Dictionary<string, IEnumerable<Message>>();

        /// <summary>
        /// This dictionary maps simulation names to lists of initial conditions tables.
        /// </summary>
        private Dictionary<string, IEnumerable<InitialConditionsTable>> initialConditions = new Dictionary<string, IEnumerable<InitialConditionsTable>>();

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model to work with</param>
        /// <param name="view">The view to attach to</param>
        /// <param name="parentPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            summaryModel = model as Summary;
            this.explorerPresenter = parentPresenter;
            summaryView = view as ISummaryView;

            // Populate the view.
            summaryView.ShowInitialConditions = true;
            summaryView.ShowInfo = true;
            summaryView.ShowWarnings = true;
            summaryView.ShowErrors = true;
            SetSimulationNamesInView();

            string simulationName = summaryView.SimulationDropDown.SelectedValue;
            messages[simulationName] = summaryModel.GetMessages(simulationName)?.ToArray();
            initialConditions[simulationName] = summaryModel.GetInitialConditions(simulationName).ToArray();

            this.UpdateView();

            summaryView.SummaryCheckBox.Checked = summaryModel.CaptureSummaryText;
            summaryView.SummaryCheckBox.Changed += OnSummaryCheckBoxChanged;
            summaryView.WarningCheckBox.Checked = summaryModel.CaptureWarnings;
            summaryView.WarningCheckBox.Changed += OnWarningCheckBoxChanged;
            summaryView.ErrorCheckBox.Checked = summaryModel.CaptureErrors;
            summaryView.ErrorCheckBox.Changed += OnErrorCheckBoxChanged;
            summaryView.FiltersChanged += OnFiltersChanged;

            // Subscribe to the simulation name changed event.
            summaryView.SimulationDropDown.Changed += this.OnSimulationNameChanged;

            // Subscribe to the view's copy event.
            //summaryView.SummaryDisplay.Copy += OnCopy;
        }

        private void OnFiltersChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void SetSimulationNamesInView()
        {
            // populate the simulation names in the view.
            IModel scopedParent = ScopingRules.FindScopedParentModel(summaryModel);

            if (scopedParent is Simulation parentSimulation)
            {
                if (scopedParent.Parent is Experiment)
                    scopedParent = scopedParent.Parent;
                else
                {
                    summaryView.SimulationDropDown.Values = new string[] { parentSimulation.Name };
                    summaryView.SimulationDropDown.SelectedValue = parentSimulation.Name;
                    return;
                }
            }

            if (scopedParent is Experiment experiment)
            {
                string[] simulationNames = experiment.GenerateSimulationDescriptions().Select(s => s.Name).ToArray();
                summaryView.SimulationDropDown.Values = simulationNames;
                if (simulationNames != null && simulationNames.Count() > 0)
                    summaryView.SimulationDropDown.SelectedValue = simulationNames.First();
            }
            else
            {
                List<ISimulationDescriptionGenerator> simulations = summaryModel.FindAllInScope<ISimulationDescriptionGenerator>().Cast<ISimulationDescriptionGenerator>().ToList();
                simulations.RemoveAll(s => s is Simulation && (s as IModel).Parent is Experiment);
                List<string> simulationNames = simulations.SelectMany(m => m.GenerateSimulationDescriptions()).Select(m => m.Name).ToList();
                simulationNames.AddRange(summaryModel.FindAllInScope<Models.Optimisation.CroptimizR>().Select(x => x.Name));
                summaryView.SimulationDropDown.Values = simulationNames.ToArray();
                if (simulationNames != null && simulationNames.Count > 0)
                    summaryView.SimulationDropDown.SelectedValue = simulationNames[0];
            }
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            summaryView.SimulationDropDown.Changed -= this.OnSimulationNameChanged;
            //summaryView.SummaryDisplay.Copy -= OnCopy;
            summaryView.SummaryCheckBox.Changed -= OnSummaryCheckBoxChanged;
            summaryView.WarningCheckBox.Changed -= OnWarningCheckBoxChanged;
            summaryView.ErrorCheckBox.Changed -= OnErrorCheckBoxChanged;
        }

        /// <summary>Populate the summary view.</summary>
        private void UpdateView()
        {
            string simulationName = summaryView.SimulationDropDown.SelectedValue;
            StringBuilder markdown = new StringBuilder();

            // Show Initial Conditions.
            if (summaryView.ShowInitialConditions)
            {
                // Fetch initial conditions from the model for this simulation name.
                if (!initialConditions.ContainsKey(simulationName))
                    initialConditions[simulationName] = summaryModel.GetInitialConditions(simulationName).ToArray();

                markdown.AppendLine(string.Join("", initialConditions[simulationName].Select(i => i.ToMarkdown())));
            }

            // Show Messages.
            if (summaryView.ShowInfo || summaryView.ShowWarnings || summaryView.ShowErrors)
            {
                // Fetch messages from the model for this simulation name.
                if (!messages.ContainsKey(simulationName))
                    messages[simulationName] = summaryModel.GetMessages(simulationName).ToArray();

                IEnumerable<Message> filteredMessages = GetFilteredMessages(simulationName);
                var groupedMessages = filteredMessages.GroupBy(m => new { m.Date, m.RelativePath });
                if (filteredMessages.Any())
                {
                    markdown.AppendLine($"## Simulation log");
                    markdown.AppendLine();
                    markdown.AppendLine(string.Join("", groupedMessages.Select(m => 
                    {
                        StringBuilder md = new StringBuilder();
                        md.AppendLine($"### {m.Key.Date:yyyy-MM-dd} {m.Key.RelativePath}");
                        md.AppendLine();
                        md.AppendLine("```");
                        foreach (Message msg in m)
                            md.AppendLine(msg.Text);
                        md.AppendLine("```");
                        md.AppendLine();
                        return md.ToString();
                    })));
                }
            }

            summaryView.SummaryDisplay.Text = markdown.ToString();
        }

        private IEnumerable<Message> GetFilteredMessages(string simulationName)
        {
            if (messages.ContainsKey(simulationName))
            {
                IEnumerable<Message> result = messages[simulationName];
                if (!summaryView.ShowInfo)
                    result = result.Where(m => m.Severity != ErrorLevel.Information);
                if (!summaryView.ShowWarnings)
                    result = result.Where(m => m.Severity != ErrorLevel.Warning);
                if (!summaryView.ShowErrors)
                    result = result.Where(m => m.Severity != ErrorLevel.Error);

                return result;
            }
            return Enumerable.Empty<Message>();
        }

        /// <summary>Handles the SimulationNameChanged event of the view control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnSimulationNameChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void OnSummaryCheckBoxChanged(object sender, EventArgs e)
        {
            ChangeProperty command = new ChangeProperty(summaryModel, "CaptureSummaryText", summaryView.SummaryCheckBox.Checked);
            explorerPresenter.CommandHistory.Add(command);
        }

        private void OnWarningCheckBoxChanged(object sender, EventArgs e)
        {
            ChangeProperty command = new ChangeProperty(summaryModel, "CaptureWarnings", summaryView.WarningCheckBox.Checked);
            explorerPresenter.CommandHistory.Add(command);
        }

        private void OnErrorCheckBoxChanged(object sender, EventArgs e)
        {
            ChangeProperty command = new ChangeProperty(summaryModel, "CaptureErrors", summaryView.ErrorCheckBox.Checked);
            explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// Event handler for the view's copy event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCopy(object sender, CopyEventArgs e)
        {
            this.explorerPresenter.SetClipboardText(e.Text, "CLIPBOARD");
        }
    }
}