using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace SparkleXrm.Tasks
{
    /// <summary>
    /// Wraps an <see cref="IOrganizationService"/> so that reads go to the server and every write is only traced.
    /// Requests whose name does not start with "Retrieve" (and WhoAmI) are treated as writes.
    /// </summary>
    public class DryRunOrganizationService : IOrganizationService
    {
        private const string Prefix = "[dry-run] ";
        private static readonly string[] NameAttributes = { "name", "uniquename", "typename" };

        private readonly IOrganizationService _inner;
        private readonly ITrace _trace;

        public DryRunOrganizationService(IOrganizationService inner, ITrace trace)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        }

        public int Creates { get; private set; }
        public int Updates { get; private set; }
        public int Deletes { get; private set; }
        public int OtherRequests { get; private set; }

        public Guid Create(Entity entity)
        {
            Creates++;
            _trace.WriteLine(Prefix + "Would create {0}", Describe(entity));
            return entity.Id != Guid.Empty ? entity.Id : Guid.NewGuid();
        }

        public void Update(Entity entity)
        {
            Updates++;
            _trace.WriteLine(Prefix + "Would update {0}", Describe(entity));
        }

        public void Delete(string entityName, Guid id)
        {
            Deletes++;
            _trace.WriteLine(Prefix + "Would delete {0} {1}", entityName, id);
        }

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            OtherRequests++;
            _trace.WriteLine(Prefix + "Would associate {0} {1} via '{2}'", entityName, entityId, relationship.SchemaName);
        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            OtherRequests++;
            _trace.WriteLine(Prefix + "Would disassociate {0} {1} via '{2}'", entityName, entityId, relationship.SchemaName);
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            return _inner.Retrieve(entityName, id, columnSet);
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            return _inner.RetrieveMultiple(query);
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            if (IsRead(request))
            {
                return _inner.Execute(request);
            }

            switch (request)
            {
                case CreateRequest create:
                    var response = new CreateResponse();
                    response.Results["id"] = Create(create.Target);
                    return response;
                case UpdateRequest update:
                    Update(update.Target);
                    return new UpdateResponse();
                case DeleteRequest delete:
                    Delete(delete.Target.LogicalName, delete.Target.Id);
                    return new DeleteResponse();
            }

            OtherRequests++;
            _trace.WriteLine(Prefix + "Would execute {0}{1}", request.RequestName, DescribeParameters(request));
            return new OrganizationResponse { ResponseName = request.RequestName };
        }

        /// <summary>
        /// Writes the number of skipped writes.
        /// </summary>
        public void WriteSummary()
        {
            _trace.WriteLine(Prefix + "{0} create(s), {1} update(s), {2} delete(s), {3} other request(s). Nothing was written.",
                Creates, Updates, Deletes, OtherRequests);
        }

        private static bool IsRead(OrganizationRequest request)
        {
            var name = request.RequestName ?? string.Empty;
            return name.StartsWith("Retrieve", StringComparison.OrdinalIgnoreCase)
                || name.Equals("WhoAmI", StringComparison.OrdinalIgnoreCase);
        }

        private static string Describe(Entity entity)
        {
            var name = NameAttributes
                .Select(a => entity.Attributes.Contains(a) ? entity[a] as string : null)
                .FirstOrDefault(n => !string.IsNullOrEmpty(n));
            var id = entity.Id != Guid.Empty ? " " + entity.Id : string.Empty;
            return name != null ? $"{entity.LogicalName} '{name}'{id}" : $"{entity.LogicalName}{id}";
        }

        private static string DescribeParameters(OrganizationRequest request)
        {
            var values = request.Parameters
                .Where(p => p.Value is string || p.Value is Guid || p.Value is int || p.Value is bool)
                .Select(p => $"{p.Key}={p.Value}")
                .ToList();
            return values.Count > 0 ? " (" + string.Join(", ", values) + ")" : string.Empty;
        }
    }
}
