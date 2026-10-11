using System.Text.Json.Nodes;
using static CampusFix.Api.Services.J;
using static CampusFix.Api.Services.Permissions;

namespace CampusFix.Api.Services;
public partial class CampusService
{
    List<JsonObject>? knowledgeConfig;
    async Task<List<JsonObject>> KnowledgeConfig()=>knowledgeConfig??=await ActiveRecords("config");
    async Task<HashSet<string>> KnowledgeAreas()
    {
        var config=await KnowledgeConfig();
        return config.Where(c=>c.S("kind")=="areas"&&c.B("activo")&&
            (u.S("role")=="Superadministrador"||u.S("role")=="Administrador"&&p.A("managedAreas").Has(c.S("id"))||
             EligibleForArea(u,c.S("id"),config))).Select(c=>c.S("id")).ToHashSet();
    }
    async Task<string> KnowledgeArea(JsonObject article)
    {
        // Explicit snapshots and service relationships, never requester membership or title keywords.
        if(article.S("responsibleAreaId")!="")return article.S("responsibleAreaId");
        if(article.S("sourceTicketId")!=""){
            var ticket=await s.Get("tickets",article.S("sourceTicketId"));
            if(ticket.S("responsibleAreaId")!="")return ticket.S("responsibleAreaId");
        }
        var config=await KnowledgeConfig();
        var serviceId=article.S("serviceId");
        if(serviceId==""&&article.S("categoryId")!="")serviceId=config.FirstOrDefault(c=>c.S("kind")=="categorias"&&c.S("id")==article.S("categoryId")).S("serviceId");
        return config.FirstOrDefault(c=>c.S("kind")=="servicios"&&c.S("id")==serviceId).S("responsibleAreaId");
    }
    async Task<bool> KnowledgeScopeAllowed(JsonObject article)
        =>u.S("role")=="Superadministrador"||(await KnowledgeAreas()).Contains(await KnowledgeArea(article));
    async Task<bool> CanReadKnowledge(JsonObject article)=>ReadArticle(article,p)&&await KnowledgeScopeAllowed(article);
    async Task<List<JsonObject>> VisibleKnowledge()
    {
        var result=new List<JsonObject>();
        foreach(var article in await ActiveRecords("articles"))if(await CanReadKnowledge(article))result.Add(article);
        return result;
    }
}
