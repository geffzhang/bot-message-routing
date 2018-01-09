﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Underscore.Bot.Models;
using Underscore.Bot.Models.Azure;
using Underscore.Bot.Utils;

namespace Underscore.Bot.MessageRouting.DataStore.Azure
{
    /// <summary>
    /// Routing data manager that stores the data in Azure Table Storage.
    /// 
    /// See IRoutingDataManager and AbstractRoutingDataManager for general documentation of
    /// properties and methods.
    /// </summary>
    [Serializable]
    public class AzureTableStorageRoutingDataManager : AbstractRoutingDataManager
    {
        protected const string TableNameParties = "Parties";
        protected const string TableNameConnections = "Connections";
        protected const string PartitionKey = "PartitionKey";

        protected CloudTable _partiesTable;
        protected CloudTable _connectionsTable;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connectionString">The connection string associated with an Azure Table Storage.</param>
        /// <param name="globalTimeProvider">The global time provider for providing the current
        /// time for various events such as when a connection is requested.</param>
        public AzureTableStorageRoutingDataManager(string connectionString, GlobalTimeProvider globalTimeProvider = null)
            : base(globalTimeProvider)
        {
            _partiesTable = AzureStorageHelper.GetTable(connectionString, TableNameParties);
            _connectionsTable = AzureStorageHelper.GetTable(connectionString, TableNameConnections);
        }

        public override IList<Party> GetUserParties()
        {
            List<PartyEntity> partyEntities = null;

            try
            {
                partyEntities =
                    _partiesTable.ExecuteQuery(new TableQuery<PartyEntity>())
                        .Where(x => x.PartyEntityType == PartyEntityType.User.ToString()).ToList();
            }
            catch (StorageException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to retrieve the user parties: {e.Message}");
                return new List<Party>();
            }

            return ToPartyList(partyEntities);
        }

        public override IList<Party> GetBotParties()
        {
            List<PartyEntity> partyEntities = null;

            try
            {
                partyEntities =
                    _partiesTable.ExecuteQuery(new TableQuery<PartyEntity>())
                        .Where(x => x.PartyEntityType == PartyEntityType.Bot.ToString()).ToList();
            }
            catch (StorageException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to retrieve the bot parties: {e.Message}");
                return new List<Party>();
            }

            return ToPartyList(partyEntities);
        }

        public override IList<Party> GetAggregationParties()
        {
            List<PartyEntity> partyEntities = null;

            try
            {
                partyEntities =
                    _partiesTable.ExecuteQuery(new TableQuery<PartyEntity>())
                        .Where(x => x.PartyEntityType == PartyEntityType.Aggregation.ToString()).ToList();
            }
            catch (StorageException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to retrieve the aggregation parties: {e.Message}");
                return new List<Party>();
            }

            return ToPartyList(partyEntities);
        }

        public override IList<Party> GetPendingRequests()
        {
            List<PartyEntity> partyEntities = null;

            try
            {
                partyEntities =
                    _partiesTable.ExecuteQuery(new TableQuery<PartyEntity>())
                        .Where(x => x.PartyEntityType == PartyEntityType.PendingRequest.ToString()).ToList();
            }
            catch (StorageException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to retrieve the pending requests: {e.Message}");
                return new List<Party>();
            }

            return ToPartyList(partyEntities);
        }

        public override Dictionary<Party, Party> GetConnectedParties()
        {
            Dictionary<Party, Party> connectedParties = new Dictionary<Party, Party>();
            List<ConnectionEntity> connectionEntities = null;

            try
            { 
                connectionEntities =
                    _connectionsTable.ExecuteQuery(new TableQuery<ConnectionEntity>()).ToList();
            }
            catch (StorageException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to retrieve the connected parties: {e.Message}");
                return connectedParties; // Return empty dictionary
            }

            foreach (var connectionEntity in connectionEntities)
            {
                connectedParties.Add(
                    JsonConvert.DeserializeObject<PartyEntity>(connectionEntity.Owner).ToParty(),
                    JsonConvert.DeserializeObject<PartyEntity>(connectionEntity.Client).ToParty());
            }

            return connectedParties;
        }

        public override void DeleteAll()
        {
            base.DeleteAll();

            try
            {
                var partyEntities = _partiesTable.ExecuteQuery(new TableQuery<PartyEntity>());

                foreach (var partyEntity in partyEntities)
                {
                    AzureStorageHelper.DeleteEntry<PartyEntity>(
                        _partiesTable, partyEntity.PartitionKey, partyEntity.RowKey);
                }
            }
            catch (StorageException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete entries: {e.Message}");
                return;
            }

            var connectionEntities = _connectionsTable.ExecuteQuery(new TableQuery<ConnectionEntity>());

            foreach (var connectionEntity in connectionEntities)
            {
                AzureStorageHelper.DeleteEntry<ConnectionEntity>(
                    _connectionsTable, connectionEntity.PartitionKey, connectionEntity.RowKey);
            }
        }

        protected override bool ExecuteAddParty(Party partyToAdd, bool isUser)
        {
            return AzureStorageHelper.Insert<PartyEntity>(
                _partiesTable,
                new PartyEntity(partyToAdd, isUser ? PartyEntityType.User : PartyEntityType.Bot));
        }

        protected override bool ExecuteRemoveParty(Party partyToRemove, bool isUser)
        {
            return AzureStorageHelper.DeleteEntry<PartyEntity>(
                _partiesTable,
                PartyEntity.CreatePartitionKey(partyToRemove, isUser ? PartyEntityType.User : PartyEntityType.Bot),
                PartyEntity.CreateRowKey(partyToRemove));
        }

        protected override bool ExecuteAddAggregationParty(Party aggregationPartyToAdd)
        {
            return AzureStorageHelper.Insert<PartyEntity>(
                _partiesTable, new PartyEntity(aggregationPartyToAdd, PartyEntityType.Aggregation));
        }

        protected override bool ExecuteRemoveAggregationParty(Party aggregationPartyToRemove)
        {
            var partyEntitiesToRemove = GetPartyEntitiesByPropertyNameAndValue(
                PartitionKey,
                PartyEntity.CreatePartitionKey(aggregationPartyToRemove, PartyEntityType.Aggregation))
                    .FirstOrDefault();

            return AzureStorageHelper.DeleteEntry<PartyEntity>(
                _partiesTable, partyEntitiesToRemove.PartitionKey, partyEntitiesToRemove.RowKey);
        }

        protected override bool ExecuteAddPendingRequest(Party requestorParty)
        {
            return AzureStorageHelper.Insert<PartyEntity>(
                _partiesTable, new PartyEntity(requestorParty, PartyEntityType.PendingRequest));
        }

        protected override bool ExecuteRemovePendingRequest(Party requestorParty)
        {
            return AzureStorageHelper.DeleteEntry<PartyEntity>(
                _partiesTable,
                PartyEntity.CreatePartitionKey(requestorParty, PartyEntityType.PendingRequest),
                PartyEntity.CreateRowKey(requestorParty));
        }

        protected override bool ExecuteAddConnection(Party conversationOwnerParty, Party conversationClientParty)
        {
            return AzureStorageHelper.Insert<ConnectionEntity>(_connectionsTable, new ConnectionEntity()
            {
                PartitionKey = conversationClientParty.ConversationAccount.Id,
                RowKey = conversationOwnerParty.ConversationAccount.Id,
                Client = JsonConvert.SerializeObject(new PartyEntity(conversationClientParty, PartyEntityType.Client)),
                Owner = JsonConvert.SerializeObject(new PartyEntity(conversationOwnerParty, PartyEntityType.Owner))
            });
        }

        protected override bool ExecuteRemoveConnection(Party conversationOwnerParty)
        {
            Dictionary<Party, Party> connectedParties = GetConnectedParties();

            if (connectedParties != null && connectedParties.Remove(conversationOwnerParty))
            {
                Party conversationClientParty = GetConnectedCounterpart(conversationOwnerParty);

                return AzureStorageHelper.DeleteEntry<ConnectionEntity>(
                    _connectionsTable,
                    conversationClientParty.ConversationAccount.Id,
                    conversationOwnerParty.ConversationAccount.Id);
            }

            return false;
        }

        /// <summary>
        /// Resolves the parties in the party (cloud) table by the given property name and value.
        /// </summary>
        /// <param name="propertyName">The property name for the filter.</param>
        /// <param name="value">Party property values to match.</param>
        /// <returns>The party entities in the table matching the given property name and value.</returns>
        protected virtual IEnumerable<PartyEntity> GetPartyEntitiesByPropertyNameAndValue(string propertyName, string value)
        {
            TableQuery<PartyEntity> tableQuery =
                new TableQuery<PartyEntity>()
                    .Where(TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.Equal, value));

            return _partiesTable.ExecuteQuery(tableQuery);
        }

        /// <summary>
        /// Converts the given entities into a party list.
        /// </summary>
        /// <param name="partyEntities">The entities to convert.</param>
        /// <returns>A newly created list of parties based on the given entities.</returns>
        protected virtual List<Party> ToPartyList(IEnumerable<PartyEntity> partyEntities)
        {
            List<Party> partyList = new List<Party>();

            foreach (var partyEntity in partyEntities)
            {
                partyList.Add(partyEntity.ToParty());
            }

            return partyList.ToList();
        }
    }
}
