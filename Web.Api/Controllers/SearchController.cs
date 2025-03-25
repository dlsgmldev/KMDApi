using KDMApi.DataContexts;
using KDMApi.Models.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using KDMApi.Models.Helper;
using Elasticsearch.Net;
using KDMApi.Models.Km;

namespace KDMApi.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class SearchController : Controller
    {
        private readonly DefaultContext _context;
        private DataOptions _options;
        public SearchController(DefaultContext context, Microsoft.Extensions.Options.IOptions<DataOptions> options)
        {
            _context = context;
            _options = options.Value;
        }

        /**
         * @api {get} /search/{tribeId}/{doctype}/{filetype}/{client}/{search}/{page}/{perPage} Search
         * @apiVersion 1.0.0
         * @apiName SearchDocument
         * @apiGroup Search
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} tribeId           0 untuk semua, atau tribeId yang diinginkan
         * @apiParam {Number} doctype           0 untuk semua, 1 untuk Deliverables, 2 untuk Pre-sales
         * @apiParam {String} filetype          * untuk semua, ppt untuk Powerpoint, xls untuk Excel, doc untuk Word, pdf untuk PDF
         * @apiParam {String} client            * untuk semua, atau nama client yang ingin dicari
         * @apiParam {String} search            kata yang ingin dicari
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "entries": [
         *           {
         *               "id": 96725,
         *               "projectId": 709,
         *               "projectName": "CERTIFIED HUMAN RESOUCES PROGRAM EXECUTIVE",
         *               "clientId": 1258,
         *               "client": "BANK OCBC NISP Tbk, PT",
         *               "startDate": "2014-10-24T00:00:00",
         *               "endDate": "2014-12-15T00:00:00",
         *               "tribeId": 2,
         *               "isDeliverables": true,
         *               "filename": "Workshop Innovative Reward OCBC CHRPE 2014_10_24 Okt-12 Dec.pptx",
         *               "fileType": "pptx",
         *               "content": "BANK OCBC NISP Tbk, PT   CERTIFIED HUMAN RESOUCES PROGRAM EXECUTIVE Slide 0 INNOVATIVE REWARD AND RE...",
         *               "lastUpdated": "2021-10-05T18:17:19.6347263+07:00"
         *           },
         *           {
         *               "id": 105510,
         *               "projectId": 727,
         *               "projectName": "CERTIFIED HUMAN RESOURCE  PROGRAM EXECUTIVE",
         *               "clientId": 1313,
         *               "client": "BANK TABUNGAN PENSIUNAN NASIONAL Tbk, PT (BTPN)",
         *               "startDate": "2015-01-19T00:00:00",
         *               "endDate": "2015-01-21T00:00:00",
         *               "tribeId": 2,
         *               "isDeliverables": true,
         *               "filename": "Workshop Innovative Reward CHRPE BTPN  2015_04_13-15 HS.pptx",
         *               "fileType": "pptx",
         *               "content": "BANK TABUNGAN PENSIUNAN NASIONAL Tbk, PT (BTPN) FINACIAL SERVICE CERTIFIED HUMAN RESOURCE  PROGRAM E...",
         *               "lastUpdated": "2021-10-05T18:17:19.6347263+07:00"
         *           }
         *       ],
         *       "info": {
         *           "page": 1,
         *           "perPage": 10,
         *           "total": 2
         *       }
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("{tribeId}/{doctype}/{filetype}/{client}/{search}/{page}/{perPage}")]
        public async Task<ActionResult<SearchResponse>> GetElasticEntries(int tribeId, int doctype, string filetype, string client, string search, int page, int perPage)
        {
            if (string.IsNullOrEmpty(search)) return BadRequest(new { error = "Search string cannot be null or empty." });

            SearchResponse response = new SearchResponse();

            Func<ViewSearchItem, bool> SearchPredicate = i =>
            {
                bool a = tribeId == 0 || i.TribeId == tribeId;
                bool b = doctype == 0 || (doctype == 1 && i.ProjectId != 0 && !i.Onegml) || (doctype == 2 && i.ProjectId == 0 && i.Onegml);
                bool c = filetype.Equals("*") || i.Filetype.Contains(filetype);
                bool d = client.Equals("*") || i.Client.Contains(client);
                return a && b && c;
            };

            var query = from si in _context.ViewSearchItems
                        where SearchPredicate(si) && si.Filename.Contains(search)
                        orderby si.TribeId
                        select si;

            int total = query.Count();
            response.Items = query.Skip(perPage * (page - 1)).Take(perPage).ToList<ViewSearchItem>();
            response.Info = new SearchInfo(page, perPage, total);

            return response;


            /*            
            if (!filetype.EndsWith("*")) filetype += "*";
            if (!client.EndsWith("*")) client += "*";

                        var settings = new ConnectionSettings(new Uri("http://localhost:9200")).BasicAuthentication(_options.ElasticUsername, _options.ElasticPassword).DefaultIndex("entries");

                        var elastic = new ElasticClient(settings);

                        List<string> deliverables = new List<string>();
                        if (doctype == 0) deliverables.AddRange(new[] { "true", "false" });
                        else if (doctype == 1) deliverables.AddRange(new[] { "true" });
                        else deliverables.AddRange(new[] { "false" });

                        SearchResponse response = new SearchResponse();
                        long total = 0;

                        if(tribeId == 0)
                        {
                            if (doctype == 1)
                            {
                                // Deliverables only
                                var searchResponse = await elastic.SearchAsync<ElasticEntry>(s => s
                                        .From(page - 1)
                                        .Size(perPage)
                                        .Query(q => q
                                            .Bool(b => b
                                                .Must(mu =>
                                                    mu.MultiMatch(m => m.Fields(f => f.Field(p => p.Content).Field("Filename")).Query(search)) &&
                                                    mu.Wildcard(m => m.Boost(1.1).Field(f => f.Client).Value(client)) &&
                                                    mu.Wildcard(m => m.Boost(1.1).Field(f => f.FileType).Value(filetype))
                                                )
                                                .MustNot(fi =>
                                                    fi.Wildcard(t => t.Field(f => f.Filename).Value("*proposal*")) &&
                                                    fi.Wildcard(t => t.Field(f => f.Filename).Value("*sertifikat*"))
                                                )
                                                .Filter(fi =>
                                                    fi.Terms(t => t.Field(f => f.IsDeliverables).Terms(deliverables))
                                                )
                                            )
                                        )
                                    );

                                if (searchResponse == null || searchResponse.Documents == null)
                                {
                                    response.Entries = new List<ElasticEntry>();
                                    return response;
                                }

                                response.Entries = (List<ElasticEntry>)searchResponse.Documents;
                                total = searchResponse.Total;
                            }
                            else
                            {
                                var searchResponse = await elastic.SearchAsync<ElasticEntry>(s => s
                                    .From(page - 1)
                                    .Size(perPage)
                                    .Query(q => q
                                        .Bool(b => b
                                            .Must(mu =>
                                                mu.Match(m => m.Field(f => f.Content).Query(search)) &&
                                                mu.Wildcard(m => m.Boost(1.1).Field(f => f.Client).Value(client)) &&
                                                mu.Wildcard(m => m.Boost(1.1).Field(f => f.FileType).Value(filetype))
                                            )
                                            .MustNot(fi =>
                                                    fi.Wildcard(t => t.Field(f => f.Filename).Value("*sertifikat*"))
                                            )
                                            .Filter(fi =>
                                                fi.Terms(t => t.Field(f => f.IsDeliverables).Terms(deliverables))
                                            )
                                        )
                                    )
                                );

                                if (searchResponse == null || searchResponse.Documents == null)
                                {
                                    response.Entries = new List<ElasticEntry>();
                                    return response;
                                }

                                response.Entries = (List<ElasticEntry>)searchResponse.Documents;
                                total = searchResponse.Total;
                            }
                        }
                        else
                        {
                            if (doctype == 1)
                            {
                                // Deliverables only
                                var searchResponse = await elastic.SearchAsync<ElasticEntry>(s => s
                                        .From(page - 1)
                                        .Size(perPage)
                                        .Query(q => q
                                            .Bool(b => b
                                                .Must(mu =>
                                                    mu.Term(p => p.TribeId, tribeId) &&
                                                    mu.MultiMatch(m => m.Fields(f => f.Field(p => p.Content).Field("Filename")).Query(search)) &&
                                                    mu.Wildcard(m => m.Boost(1.1).Field(f => f.Client).Value(client)) &&
                                                    mu.Wildcard(m => m.Boost(1.1).Field(f => f.FileType).Value(filetype))
                                                )
                                                .MustNot(fi =>
                                                    fi.Wildcard(t => t.Field(f => f.Filename).Value("*proposal*")) &&
                                                    fi.Wildcard(t => t.Field(f => f.Filename).Value("*sertifikat*"))
                                                )
                                                .Filter(fi =>
                                                    fi.Terms(t => t.Field(f => f.IsDeliverables).Terms(deliverables))
                                                )
                                            )
                                        )
                                    );

                                if (searchResponse == null || searchResponse.Documents == null)
                                {
                                    response.Entries = new List<ElasticEntry>();
                                    return response;
                                }

                                response.Entries = (List<ElasticEntry>)searchResponse.Documents;
                                total = searchResponse.Total;
                            }
                            else
                            {
                                var searchResponse = await elastic.SearchAsync<ElasticEntry>(s => s
                                    .From(page - 1)
                                    .Size(perPage)
                                    .Query(q => q
                                        .Bool(b => b
                                            .Must(mu =>
                                                mu.Term(p => p.TribeId, tribeId) &&
                                                mu.Match(m => m.Field(f => f.Content).Query(search)) &&
                                                mu.Wildcard(m => m.Boost(1.1).Field(f => f.Client).Value(client)) &&
                                                mu.Wildcard(m => m.Boost(1.1).Field(f => f.FileType).Value(filetype))
                                            )
                                            .MustNot(fi =>
                                                    fi.Wildcard(t => t.Field(f => f.Filename).Value("*sertifikat*"))
                                            )
                                            .Filter(fi =>
                                                fi.Terms(t => t.Field(f => f.IsDeliverables).Terms(deliverables))
                                            )
                                        )
                                    )
                                );

                                if (searchResponse == null || searchResponse.Documents == null)
                                {
                                    response.Entries = new List<ElasticEntry>();
                                    return response;
                                }

                                response.Entries = (List<ElasticEntry>)searchResponse.Documents;
                                total = searchResponse.Total;
                            }

                        }

                        if (response.Entries != null) response.Entries.ForEach(a => a.Content = a.Content.Substring(0, 100) + "...");
                        response.Info = new SearchInfo(page, perPage, total);
                        return response;
            */
        }
    }
}
/*
q
.MultiMatch(c => c
    .Fields(f => f.Field(p => p.Description).Field("myOtherField"))
    .Query("hello world")
    .Analyzer("standard")
    .Boost(1.1)
    .Slop(2)
    .Fuzziness(Fuzziness.Auto)
    .PrefixLength(2)
    .MaxExpansions(2)
    .Operator(Operator.Or)
    .MinimumShouldMatch(2)
    .FuzzyRewrite(MultiTermQueryRewrite.ConstantScoreBoolean)
    .TieBreaker(1.1)
    .CutoffFrequency(0.001)
    .Lenient()
    .ZeroTermsQuery(ZeroTermsQuery.All)
    .Name("named_query")
    .AutoGenerateSynonymsPhraseQuery(false)
)
 */ 