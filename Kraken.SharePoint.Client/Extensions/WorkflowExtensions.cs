/* Older versions of CSOM did not include this API */
#if !DOTNET_V35

namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint.Client.WorkflowServices;

  public static class KrakenWorkflowExtensions {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetList"></param>
    /// <param name="workflowName">Name of the SharePoint2013 List Workflow</param>
    /// <param name="subscriptionName">Name of the new Subscription (association)</param>
    /// <param name="historyList"></param>
    /// <param name="taskList"></param>
    public static void AssociateWorkflow2013(this List targetList, 
      string workflowName,
      string subscriptionName,
      List historyList,
      List taskList,
      WorkflowEvents events
    ) {

      //GUID of list on which to create the subscription (association);
      Guid targetListGuid = targetList.Id;
        
      if (string.IsNullOrEmpty(subscriptionName))
        subscriptionName = workflowName + " Workflow Association";

      //Workflow Lists
      //string workflowHistoryListID = historyList.Id.ToString();
      //string taskListID = taskList.Id.ToString(); 

      ClientContext clientContext = (ClientContext)targetList.Context;
      Web web = clientContext.Web;

      // TODO simplify this by creating a utility function to get the workflow definition

      //Workflow Services Manager which will handle all the workflow interaction.
      WorkflowServicesManager wfServicesManager = new WorkflowServicesManager(clientContext, web);
      //Deployment Service which holds all the Workflow Definitions deployed to the site
      WorkflowDeploymentService wfDeploymentService = wfServicesManager.GetWorkflowDeploymentService();
      //Get all the definitions from the Deployment Service, or get a specific definition using the GetDefinition method.
      WorkflowDefinitionCollection wfDefinitions = wfDeploymentService.EnumerateDefinitions(false);
      clientContext.Load(wfDefinitions, wfDefs => wfDefs.Where(wfd => wfd.DisplayName == workflowName));
      clientContext.ExecuteQuery();
      WorkflowDefinition wfDefinition = wfDefinitions.First();

      //The Subscription service is used to get all the Associations currently on the SPSite
      WorkflowSubscriptionService wfSubscriptionService = wfServicesManager.GetWorkflowSubscriptionService();

      //The subscription (association)
      WorkflowSubscription wfSubscription = new WorkflowSubscription(clientContext);
      wfSubscription.DefinitionId = wfDefinition.Id;
      wfSubscription.Enabled = true;
      wfSubscription.Name = subscriptionName;

      List<string> startupOptions = new List<string>();

      // TODO replace this with loop through events
      // automatic start
      startupOptions.Add("ItemAdded");
      startupOptions.Add("ItemUpdated");
      // manual start
      startupOptions.Add("WorkflowStart");

      // set the workflow start settings
      wfSubscription.EventTypes = startupOptions;

      // set the associated task and history lists
      wfSubscription.SetProperty("HistoryListId", historyList.Id.ToString());
      wfSubscription.SetProperty("TaskListId", taskList.Id.ToString());

      //Create the Association
      wfSubscriptionService.PublishSubscriptionForList(wfSubscription, targetListGuid);

      clientContext.ExecuteQuery();
    }

  }
}
#endif