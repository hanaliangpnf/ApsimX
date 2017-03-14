﻿using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	///<summary>
	/// Manger for all activities available to the model
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WholeFarm))]
	public class ActivitiesHolder: WFModel
	{
		/// <summary>
		/// List of the all the Activities.
		/// </summary>
		[XmlIgnore]
		private List<IModel> activities;

		/// <summary>
		/// Function to return an activity from the list of available activities.
		/// </summary>
		/// <param name="Name"></param>
		/// <returns>Activity with requested name or null</returns>
		public IModel GetByName(string Name)
		{
			return activities.Find(x => x.Name == Name);
		}

		private void BindEvents(List<IModel> root)
		{
			foreach (var item in root.Where(a => a.GetType().IsSubclassOf(typeof(WFActivityBase))))
			{
				if (item.GetType() != typeof(ActivityFolder))
				{
					(item as WFActivityBase).ResourceShortfallOccurred += ActivitiesHolder_ResourceShortfallOccurred;
				}
				BindEvents(item.Children.Cast<IModel>().ToList());
			}
		}

		/// <summary>
		/// Last resource request that was in defecit
		/// </summary>
		public ResourceRequest LastShortfallResourceRequest { get; set; }

		private void ActivitiesHolder_ResourceShortfallOccurred(object sender, EventArgs e)
		{
			// save resource request
			LastShortfallResourceRequest = (e as ResourceRequestEventArgs).Request;
			// call resourceShortfallEventhandler
			OnShortfallOccurred(e);
		}

		/// <summary>
		/// Resource shortfall occured event handler
		/// </summary>
		public event EventHandler ResourceShortfallOccurred;

		/// <summary>
		/// Shortfall occurred 
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnShortfallOccurred(EventArgs e)
		{
			if (ResourceShortfallOccurred != null)
				ResourceShortfallOccurred(this, e);
		}

		/// <summary>
		/// Function to return an activity from the list of available activities.
		/// </summary>
		/// <param name="activity"></param>
		/// <param name="Name"></param>
		/// <returns></returns>
		private IModel SearchForNameInActivity(Model activity, string Name)
		{
			IModel found = activity.Children.Find(x => x.Name == Name);
			if (found != null) return found;

			foreach (var child in activity.Children)
			{
				found = SearchForNameInActivity(child, Name);
				if (found != null) return found;
			}
			return null;
		}

		/// <summary>
		/// Function to return an activity from the list of available activities.
		/// </summary>
		/// <param name="Name"></param>
		/// <returns></returns>
		public IModel SearchForNameInActivities(string Name)
		{
			IModel found = Children.Find(x => x.Name == Name);
			if (found != null) return found;

			foreach (var child in Children)
			{
				found = SearchForNameInActivity(child, Name);
				if (found != null) return found;
			}
			return null;
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			activities = Apsim.Children(this, typeof(IModel));
			BindEvents(activities);
		}

		/// <summary>An event handler to allow to call all Activities in tree to request their resources in order.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFGetResourcesRequired")]
		private void OnGetResourcesRequired(object sender, EventArgs e)
		{
			foreach (WFActivityBase child in Children.Where(a => a.GetType().IsSubclassOf(typeof(WFActivityBase))))
			{
				child.GetResourcesForAllActivities();
			}
		}


	}
}
