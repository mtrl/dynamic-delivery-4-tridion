﻿using System;
using System.Linq;
using T = Tridion.ContentDelivery.DynamicContent;
using Tridion.ContentDelivery.DynamicContent;
using Query = Tridion.ContentDelivery.DynamicContent.Query.Query;
using TMeta = Tridion.ContentDelivery.Meta;
using DD4T.ContentModel;
//using DD4T.Utils;
using System.Collections.Generic;
using DD4T.ContentModel.Contracts.Providers;
using System.Collections;
using DD4T.ContentModel.Querying;
using DD4T.Utils;

namespace DD4T.Providers.SDLTridion2011
{
    /// <summary>
    /// 
    /// </summary>
    public class TridionComponentProvider : BaseProvider, IComponentProvider
    {
        Dictionary<int, T.ComponentPresentationFactory> _cpFactoryList = null;
        Dictionary<int, TMeta.ComponentMetaFactory> _cmFactoryList = null;

        private object lock1 = new object();
        private object lock2 = new object();

        private string selectByComponentTemplateId;
        private string selectByOutputFormat;
        public TridionComponentProvider()
        {
            selectByComponentTemplateId = ConfigurationHelper.SelectComponentByComponentTemplateId;
            selectByOutputFormat = ConfigurationHelper.SelectComponentByOutputFormat;
            _cmFactoryList = new Dictionary<int, TMeta.ComponentMetaFactory>();
        }

        /// <summary>
        /// Returns the Component contents which could be found. Components that couldn't be found don't appear in the list. 
        /// </summary>
        /// <param name="componentUris"></param>
        /// <returns></returns>
        public List<string> GetContentMultiple(string[] componentUris)
        {
            TcmUri uri = new TcmUri(componentUris.First());
            ComponentPresentationFactory cpFactory = new ComponentPresentationFactory(uri.PublicationId);
            var components =
                componentUris
                .Select(componentUri => (Tridion.ContentDelivery.DynamicContent.ComponentPresentation)cpFactory.FindAllComponentPresentations(componentUri)[0])
                .Where(cp => cp != null)
                .Select(cp => cp.Content)
                .ToList();

            return components;

        }

        public IList<string> FindComponents(IQuery query)
        {
            if (! (query is ITridionQueryWrapper))
                throw new InvalidCastException("Cannot execute query because it is not based on " + typeof(ITridionQueryWrapper).Name);

            Query tridionQuery = ((ITridionQueryWrapper)query).ToTridionQuery();
            return tridionQuery.ExecuteQuery();
        }


        public DateTime GetLastPublishedDate(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            TMeta.IComponentMeta cmeta = GetComponentMetaFactory(tcmUri.PublicationId).GetMeta(tcmUri.ItemId);
            return cmeta == null ? DateTime.Now : cmeta.LastPublicationDate;
        }

        private TMeta.ComponentMetaFactory GetComponentMetaFactory(int publicationId)
        {
            if (_cmFactoryList.ContainsKey(publicationId))
                return _cmFactoryList[publicationId];

            lock (lock1)
            {
                if (!_cmFactoryList.ContainsKey(publicationId)) // we must test again, because in the mean time another thread might have added a record to the dictionary!
                {
                    _cmFactoryList.Add(publicationId, new TMeta.ComponentMetaFactory(publicationId));
                }
            }
            return _cmFactoryList[publicationId];
        }


        public string GetContent(string uri, string templateUri = "")
        {
            TcmUri tcmUri = new TcmUri(uri);

            TcmUri templateTcmUri = new TcmUri(templateUri);

            Tridion.ContentDelivery.DynamicContent.ComponentPresentationFactory cpFactory = new ComponentPresentationFactory(tcmUri.PublicationId);
            Tridion.ContentDelivery.DynamicContent.ComponentPresentation cp = null;

            if (!String.IsNullOrEmpty(templateUri))
            {
                cp = cpFactory.GetComponentPresentation(tcmUri.ItemId, templateTcmUri.ItemId);
                if (cp != null)
                    return cp.Content;
            }
            if (!string.IsNullOrEmpty(selectByComponentTemplateId))
            {
                cp = cpFactory.GetComponentPresentation(tcmUri.ItemId, Convert.ToInt32(selectByComponentTemplateId));
                if (cp != null)
                    return cp.Content;
            }
            if (!string.IsNullOrEmpty(selectByOutputFormat))
            {
                cp = cpFactory.GetComponentPresentationWithOutputFormat(tcmUri.ItemId, selectByOutputFormat);
                if (cp != null)
                    return cp.Content;
            }
            IList cps = cpFactory.FindAllComponentPresentations(tcmUri.ItemId);

            foreach (Tridion.ContentDelivery.DynamicContent.ComponentPresentation _cp in cps)
            {
                if (_cp != null)
                {
                    if (_cp.Content.Contains("<Component"))
                    {
                        return _cp.Content;
                    }
                }
            }
            return string.Empty;
        }
    }
}
