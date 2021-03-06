﻿using PnP.Framework.Extensions;
using PnP.Framework.Provisioning.Model;
using PnP.Framework.Provisioning.Providers.Xml.Resolvers;
using PnP.Framework.Provisioning.Providers.Xml.Resolvers.V201705;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace PnP.Framework.Provisioning.Providers.Xml.Serializers.V201705
{
    /// <summary>
    /// Class to serialize/deserialize the Content Types
    /// </summary>
    [TemplateSchemaSerializer(SerializationSequence = 1000, DeserializationSequence = 1000,
        MinimalSupportedSchemaVersion = XMLPnPSchemaVersion.V201705,
        Scope = SerializerScope.ProvisioningTemplate)]
    internal class ContentTypesSerializer : PnPBaseSchemaSerializer<ContentType>
    {
        public override void Deserialize(object persistence, ProvisioningTemplate template)
        {
            var contentTypes = persistence.GetPublicInstancePropertyValue("ContentTypes");

            if (contentTypes != null)
            {
                var expressions = new Dictionary<Expression<Func<ContentType, Object>>, IResolver>
                {

                    // Define custom resolver for FieldRef.ID because needs conversion from String to GUID
                    { c => c.FieldRefs[0].Id, new FromStringToGuidValueResolver() },
                    //document template
                    { c => c.DocumentTemplate, new ExpressionValueResolver((s, v) => v.GetPublicInstancePropertyValue("TargetName")) },
                    //document set template
                    { c => c.DocumentSetTemplate, new PropertyObjectTypeResolver<ContentType>(ct => ct.DocumentSetTemplate) },

                    // TODO: AllowedContentTypes is not a collection but a single

                    //document set template - allowed content types
                    {
                        c => c.DocumentSetTemplate.AllowedContentTypes,
                        new DocumentSetTemplateAllowedContentTypesFromSchemaToModelTypeResolver()
                    },

                    //document set template - remove existing content types
                    {
                        c => c.DocumentSetTemplate.RemoveExistingContentTypes,
                        new RemoveExistingContentTypesFromSchemaToModelValueResolver()
                    },

                    //document set template - shared fields + welcome page fields
                    { c => c.DocumentSetTemplate.SharedFields[0].Id, new FromStringToGuidValueResolver() },

                    //document set template - XmlDocuments section
                    { c => c.DocumentSetTemplate.XmlDocuments, new XmlAnyFromSchemaToModelValueResolver("XmlDocuments") }
                };

                template.ContentTypes.AddRange(
                    PnPObjectsMapper.MapObjects<ContentType>(contentTypes,
                            new CollectionFromSchemaToModelTypeResolver(typeof(ContentType)),
                            expressions,
                            recursive: true)
                            as IEnumerable<ContentType>);
            }
        }

        public override void Serialize(ProvisioningTemplate template, object persistence)
        {
            if (template.ContentTypes != null && template.ContentTypes.Count > 0)
            {
                var baseNamespace = PnPSerializationScope.Current?.BaseSchemaNamespace;
                var contentTypeTypeName = $"{baseNamespace}.ContentType, {PnPSerializationScope.Current?.BaseSchemaAssemblyName}";
                var contentTypeType = Type.GetType(contentTypeTypeName, true);
                var documentSetTemplateTypeName = $"{baseNamespace}.DocumentSetTemplate, {PnPSerializationScope.Current?.BaseSchemaAssemblyName}";
                var documentSetTemplateType = Type.GetType(documentSetTemplateTypeName, true);
                var documentTemplateTypeName = $"{baseNamespace}.ContentTypeDocumentTemplate, {PnPSerializationScope.Current?.BaseSchemaAssemblyName}";
                var documentTemplateType = Type.GetType(documentTemplateTypeName, true);
                var documentSetTemplateXmlDocumentsTypeName = $"{baseNamespace}.DocumentSetTemplateXmlDocuments, {PnPSerializationScope.Current?.BaseSchemaAssemblyName}";
                var documentSetTemplateXmlDocumentsType = Type.GetType(documentSetTemplateXmlDocumentsTypeName, false);

                var expressions = new Dictionary<string, IResolver>
                {

                    //document set template
                    { $"{contentTypeType.FullName}.DocumentSetTemplate", new PropertyObjectTypeResolver(documentSetTemplateType, "DocumentSetTemplate") },

                    //document set template - allowed content types
                    {
                        $"{contentTypeType.Namespace}.DocumentSetTemplate.AllowedContentTypes",
                        new DocumentSetTemplateAllowedContentTypesFromModelToSchemaTypeResolver()
                    },

                    //document set template - shared fields and welcome page fields (this expression also used to resolve fieldref collection ids because of same type name)
                    // { $"{contentTypeType.Namespace}.FieldRefBase.ID", new ExpressionValueResolver((s, v) => v != null ? v.ToString() : s?.ToString()) },
                    //document template
                    { $"{contentTypeType.FullName}.DocumentTemplate", new DocumentTemplateFromModelToSchemaTypeResolver(documentTemplateType) }
                };

                if (documentSetTemplateXmlDocumentsType != null)
                {
                    //document set template - XmlDocuments section
                    expressions.Add($"{documentSetTemplateType.FullName}.XmlDocuments", new XmlAnyFromModelToSchemalValueResolver(documentSetTemplateXmlDocumentsType));
                }

                persistence.GetPublicInstanceProperty("ContentTypes")
                    .SetValue(
                        persistence,
                        PnPObjectsMapper.MapObjects(template.ContentTypes,
                            new CollectionFromModelToSchemaTypeResolver(contentTypeType), expressions, true));
            }
        }
    }
}
