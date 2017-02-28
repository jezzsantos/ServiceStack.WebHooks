﻿using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;
using ServiceStack.Webhooks.Azure.Table;
using ServiceStack.Webhooks.ServiceModel.Types;

namespace ServiceStack.Webhooks.Azure
{
    public class AzureTableSubscriptionStore : IWebhookSubscriptionStore
    {
        internal const string DefaultTableName = "webhooksubscriptions";
        internal const string AzureConnectionStringSettingName = "AzureTableSubscriptionStore.AzureConnectionString";
        internal const string DefaultAzureConnectionString = @"UseDevelopmentStorage=true";
        private IAzureTableStorage tableStorage;

        public AzureTableSubscriptionStore()
        {
            TableName = DefaultTableName;

            AzureConnectionString = DefaultAzureConnectionString;
        }

        public AzureTableSubscriptionStore(IAppSettings settings) : this()
        {
            Guard.AgainstNull(() => settings, settings);

            AzureConnectionString = settings.Get(AzureConnectionStringSettingName, DefaultAzureConnectionString);
        }

        /// <summary>
        ///     For testing only
        /// </summary>
        internal IAzureTableStorage TableStorage
        {
            get { return tableStorage ?? (tableStorage = new AzureTableStorage(AzureConnectionString, TableName)); }
            set { tableStorage = value; }
        }

        public string AzureConnectionString { get; set; }

        public string TableName { get; set; }

        public string Add(WebhookSubscription subscription)
        {
            Guard.AgainstNull(() => subscription, subscription);

            var identity = DataFormats.CreateEntityIdentifier();
            subscription.Id = identity;

            TableStorage.Add(subscription.ToEntity());

            return identity;
        }

        public List<WebhookSubscription> Find(string userId)
        {
            return TableStorage.Find(new TableStorageQuery(@"CreatedById", QueryOperator.EQ, userId))
                .ConvertAll(x => x.FromEntity());
        }

        public WebhookSubscription Get(string userId, string eventName)
        {
            Guard.AgainstNullOrEmpty(() => eventName, eventName);

            return TableStorage.Find(new TableStorageQuery(new List<QueryPart>
                {
                    new QueryPart(@"CreatedById", QueryOperator.EQ, userId),
                    new QueryPart(@"Event", QueryOperator.EQ, eventName)
                }))
                .ConvertAll(x => x.FromEntity())
                .FirstOrDefault();
        }

        public void Update(string subscriptionId, WebhookSubscription subscription)
        {
            Guard.AgainstNullOrEmpty(() => subscriptionId, subscriptionId);
            Guard.AgainstNull(() => subscription, subscription);

            var sub = TableStorage.Get(subscriptionId);
            if (sub != null)
            {
                TableStorage.Update(subscription.ToEntity());
            }
        }

        public void Delete(string subscriptionId)
        {
            Guard.AgainstNullOrEmpty(() => subscriptionId, subscriptionId);

            var subscription = TableStorage.Get(subscriptionId);
            if (subscription != null)
            {
                TableStorage.Delete(subscription);
            }
        }

        public void Clear()
        {
            TableStorage.Empty();
        }
    }
}