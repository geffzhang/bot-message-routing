﻿using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using Underscore.Bot.Models;
using Underscore.Bot.Utils;

namespace Underscore.Bot.MessageRouting.DataStore.Local
{
    /// <summary>
    /// Routing data manager that stores the data locally.
    /// 
    /// NOTE: USE THIS CLASS ONLY FOR TESTING!
    /// Storing the data like this in production would not work since the bot can and likely will
    /// have multiple instances.
    /// 
    /// See IRoutingDataManager and AbstractRoutingDataManager for general documentation of
    /// properties and methods.
    /// </summary>
    [Serializable]
    public class InMemoryRoutingDataStore : IRoutingDataStore
    {
        #region Classes
        /// <summary>
        /// Parties that are users (not this bot).
        /// </summary>
        protected IList<ConversationReference> Users { get; set; }

        /// <summary>
        /// If the bot is addressed from different channels, its identity in terms of ID and name
        /// can vary. Those different identities are stored in this list.
        /// </summary>
        protected IList<ConversationReference> BotInstances { get; set; }

        /// <summary>
        /// Represents the channels (and the specific conversations e.g. specific channel in Slack),
        /// where the chat requests are directed. For instance, a channel could be where the
        /// customer service agents accept customer chat requests. 
        /// </summary>
        protected IList<ConversationReference> AggregationChannels{ get; set; }

        /// <summary>
        /// Represents the connections
        /// </summary>
        protected IList<Connection> Connections { get; set; }

        /// <summary>
        /// The list of parties waiting for their (conversation) requests to be accepted.
        /// </summary>
        protected List<ConnectionRequest> ConnectionRequests { get; set; }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="globalTimeProvider">The global time provider for providing the current
        /// time for various events such as when a connection is requested.</param>
        public InMemoryRoutingDataStore(GlobalTimeProvider globalTimeProvider = null)
            : base()
        {
            AggregationChannels = new List<ConversationReference>();
            Users = new List<ConversationReference>();
            BotInstances = new List<ConversationReference>();
            ConnectionRequests = new List<ConnectionRequest>();
        }

        public IList<ConversationReference> GetUsers()
        {
            List<ConversationReference> userPartiesAsList = Users as List<ConversationReference>;
            return userPartiesAsList?.AsReadOnly();
        }

        public IList<ConversationReference> GetBotInstances()
        {
            List<ConversationReference> botPartiesAsList = BotInstances as List<ConversationReference>;
            return botPartiesAsList?.AsReadOnly();
        }

        public bool AddConversationReference(ConversationReference conversationReferenceToAdd)
        {
            if (conversationReferenceToAdd.User != null)
            {
                Users.Add(conversationReferenceToAdd);
                return true;
            }
            
            if (conversationReferenceToAdd.Bot != null)
            {
                BotInstances.Add(conversationReferenceToAdd);
                return true;
            }

            return false;
        }

        public bool RemoveConversationReference(ConversationReference conversationReferenceToRemove)
        {
            if (conversationReferenceToRemove.User !=null)
            {
                return Users.Remove(conversationReferenceToRemove);
            }

            if(conversationReferenceToRemove.Bot != null)
            {
                return BotInstances.Remove(conversationReferenceToRemove);
            }

            return false;
        }

        public IList<ConversationReference> GetAggregationChannels()
        {
            List<ConversationReference> aggregationPartiesAsList = AggregationChannels as List<ConversationReference>;
            return aggregationPartiesAsList?.AsReadOnly();
        }

        public bool AddAggregationChannel(ConversationReference aggregationChannelToAdd)
        {
            AggregationChannels.Add(aggregationChannelToAdd);
            return true;
        }

        public bool RemoveAggregationChannel(ConversationReference aggregationConversationReferenceToRemove)
        {
            return AggregationChannels.Remove(aggregationConversationReferenceToRemove);
        }

        public IList<ConnectionRequest> GetConnectionRequests()
        {
            List<ConnectionRequest> connectionRequestsAsList = ConnectionRequests as List<ConnectionRequest>;
            return connectionRequestsAsList?.AsReadOnly();
        }
   
        public bool AddConnectionRequest(ConnectionRequest requestorConversationReference)
        {
            ConnectionRequests.Add(requestorConversationReference);
            return true;
        }

        public bool RemoveConnectionRequest(ConnectionRequest requestorConversationReference)
        {
            return ConnectionRequests.Remove(requestorConversationReference);
        }

        public IList<Connection> GetConnections()
        {
            List<Connection> connectionsAsList = Connections as List<Connection>;
            return connectionsAsList?.AsReadOnly();
        }

        public bool AddConnection(Connection connectionToAdd)
        {
            Connections.Add(connectionToAdd);
            return true;
        }

        public bool RemoveConnection(Connection connectionToRemove)
        {
            return Connections.Remove(connectionToRemove);
        }
    }
}