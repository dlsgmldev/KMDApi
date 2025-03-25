using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing.Charts;
using KDMApi.DataContexts;
using KDMApi.Models;
using KDMApi.Models.Helper;
using KDMApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KDMApi.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class FileController : ControllerBase
    {
        private readonly DefaultContext _context;
        private readonly FileService _fileService;
        private DataOptions _options;
        public FileController(DefaultContext context, Microsoft.Extensions.Options.IOptions<DataOptions> options, FileService fileService)
        {
            _context = context;
            _options = options.Value;
            _fileService = fileService;
        }

        /**
         * @api {get} /File/view/{type}/{id}/{userId} GET view URL
         * @apiVersion 1.0.0
         * @apiName GetFileView
         * @apiGroup File
         * @apiPermission ApiUser
         * @apiParam {Number} type          Jenis file yang mau dlihat. 1 untuk Proposal, 2 untuk Pricing, 3 untuk invoice, 4 untuk dokumen KC.
         * @apiParam {Number} id            Id dari proposal/pricing/invoice, atau id dari file di KC
         * @apiParam {Number} userId        User Id yang mau melihat file
         * 
         * @apiSuccessExample Success-Response:
         * URL untuk view file
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("view/{type}/{id}/{userId}")]
        public async Task<ActionResult<string>> GetFileView(int type, int id, int userId)
        {
            string url = "";

            if (type == 0 || id == 0) return BadRequest(new { error = "Type dan id tidak boleh 0" });
            switch (type)
            {
                case 1:         // Proposal
                    CrmDealProposal proposal = _context.CrmDealProposals.Find(id);
                    if(proposal == null)
                    {
                        return NotFound(new { error = "Proposal Id salah" });
                    }
                    if(proposal.Filename == null)
                    {
                        return NotFound(new { error = "File tidak ditemukan" });
                    }
                    KmPrepareView view = new KmPrepareView()
                    {
                        Filename = proposal.Filename,
                        DisplayFilename = proposal.OriginalFilename,
                        Filetype = System.IO.Path.GetExtension(proposal.Filename).Substring(1).ToLower(),
                        UserId = userId,
                        FileId = 0,
                        RandomId = 0,
                        Expired = new DateTime(9999, 12, 31),
                        PublicAccess = false,
                        Drive = "",
                        Path1 = proposal.RootFolder, //_options.DataRootDirectory,
                        Path2 = "deal",
                        Path3 = proposal.DealId.ToString(),
                        Path4 = "",
                        Path5 = "",
                        Path6 = "",
                        Fullpath = Path.Combine(proposal.RootFolder, "deal", proposal.DealId.ToString(), proposal.Filename)
                    };
                    _context.KmPrepareViews.Add(view);
                    _context.SaveChanges();

                    url = string.Join("", new[] { _options.DocViewerBaseURL, view.Id.ToString() });
                    break;

                case 2:         // Pricing
                    CrmDealPNL pnl = _context.CrmDealPNLs.Find(id);
                    if (pnl == null)
                    {
                        return NotFound();
                    }

                    KmPrepareView view1 = new KmPrepareView()
                    {
                        Filename = pnl.Filename,
                        DisplayFilename = pnl.OriginalFilename,
                        Filetype = System.IO.Path.GetExtension(pnl.Filename).Substring(1).ToLower(),
                        UserId = userId,
                        FileId = 0,
                        RandomId = 0,
                        Expired = new DateTime(9999, 12, 31),
                        PublicAccess = false,
                        Drive = "",
                        Path1 = pnl.RootFolder, // _options.DataRootDirectory,
                        Path2 = "deal",
                        Path3 = pnl.DealId.ToString(),
                        Path4 = "",
                        Path5 = "",
                        Path6 = "",
                        Fullpath = Path.Combine(pnl.RootFolder, "deal", pnl.DealId.ToString(), pnl.Filename)

                    };
                    _context.KmPrepareViews.Add(view1);
                    _context.SaveChanges();

                    url = string.Join("", new[] { _options.DocViewerBaseURL, view1.Id.ToString() });
                    break;

                case 3:         // Invoice
                    CrmDealInvoice invoice = _context.CrmDealInvoices.Find(id);
                    if(invoice == null)
                    {
                        return NotFound();
                    }

                    KmPrepareView view2 = new KmPrepareView()
                    {
                        Filename = invoice.Filename,
                        DisplayFilename = invoice.OriginalFilename,
                        Filetype = System.IO.Path.GetExtension(invoice.Filename).Substring(1).ToLower(),
                        UserId = userId,
                        FileId = 0,
                        RandomId = 0,
                        Expired = new DateTime(9999, 12, 31),
                        PublicAccess = false,
                        Drive = "",
                        Path1 = invoice.RootFolder, // _options.DataRootDirectory,
                        Path2 = "deal",
                        Path3 = invoice.DealId.ToString(),
                        Path4 = "",
                        Path5 = "",
                        Path6 = "",
                        Fullpath = Path.Combine(invoice.RootFolder, "deal", invoice.DealId.ToString(), invoice.Filename)

                    };
                    _context.KmPrepareViews.Add(view2);
                    _context.SaveChanges();

                    url = string.Join("", new[] { _options.DocViewerBaseURL, view2.Id.ToString() });
                    break;

                case 4:         // KC
                    if(id < 0)
                    {
                        // webinar recording
                        KmWebinarFileFolder f = _context.KmWebinarFileFolders.Find(-id);
                        if(f != null)
                        {
                            return GetWebinarFileURL(f.RootFolder, f.FolderFileName);
                        }

                    }
                    KmFile file = _context.KmFiles.Find(id);

                    if(file == null)
                    {
                        return NotFound();
                    }

                    KmPrepareView view3;
                    if (file.Onegml)
                    {
                        // Start unknown error. Sometimes the fileType is not recorded correctly.
                        if(!file.Name.EndsWith(file.FileType))
                        {
                            string ext = System.IO.Path.GetExtension(file.Name).Substring(1).ToLower();

                            string str = RenameFile(file.Filename, file.Filename + "." + ext, file.RootFolder, @"onegml", "", "", "", "");
                            if(str != null)
                            {
                                file.FileType = ext;
                                file.Filename = file.Filename + "." + ext;
                                _context.Entry(file).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            }
                        }
                        // end unknown error

                        view3 = new KmPrepareView()
                        {
                            Filename = file.Filename,
                            DisplayFilename = file.Name,
                            Filetype = System.IO.Path.GetExtension(file.Filename).Substring(1).ToLower(),
                            UserId = userId,
                            FileId = 0,
                            RandomId = 0,
                            Expired = new DateTime(9999, 12, 31),
                            PublicAccess = false,
                            Drive = "",
                            Path1 = file.RootFolder, // _options.DataRootDirectory,
                            Path2 = @"onegml",
                            Path3 = "",
                            Path4 = "",
                            Path5 = "",
                            Path6 = "",
                            Fullpath = file.Fullpath
                        };

                    }
                    else
                    {
                        KmProject project = _context.KmProjects.Find(file.ProjectId);
                        if (project == null)
                        {
                            return NotFound();
                        }

                        string parentPath = file.ParentId == 0 ? "" : file.ParentId.ToString();

                        // Start unknown error. Sometimes the fileType is not recorded correctly.
                        if (!file.Name.EndsWith(file.FileType))
                        {
                            string ext = System.IO.Path.GetExtension(file.Name).Substring(1).ToLower();

                            string str = RenameFile(file.Filename, file.Filename + "." + ext, file.RootFolder, project.TribeId.ToString(), project.ClientId.ToString(), project.YearId.ToString(), project.Id.ToString(), parentPath);
                            if(str != null)
                            {
                                file.FileType = ext;
                                file.Filename = file.Filename + "." + ext;
                                _context.Entry(file).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            }
                        }
                        // end unknown error

                        view3 = new KmPrepareView()
                        {
                            Filename = file.Filename,
                            DisplayFilename = file.Name,
                            Filetype = System.IO.Path.GetExtension(file.Filename).Substring(1).ToLower(),
                            UserId = userId,
                            FileId = 0,
                            RandomId = 0,
                            Expired = new DateTime(9999, 12, 31),
                            PublicAccess = false,
                            Drive = "",
                            Path1 = file.RootFolder, // _options.DataRootDirectory,
                            Path2 = project.TribeId.ToString(),
                            Path3 = project.ClientId.ToString(),
                            Path4 = project.YearId.ToString(),
                            Path5 = project.Id.ToString(),
                            Path6 = parentPath,
                            Fullpath = file.Fullpath
                        };

                    }

                    if (view3 != null)
                    {
                        _context.KmPrepareViews.Add(view3);
                        _context.SaveChanges();
                    }
                    else
                    {
                        return BadRequest(new { error = "Unknown error" });
                    }

                    if (file.FileType.Contains("mp4"))
                    {
                        string src = Path.Combine(view3.Path1, view3.Path2, view3.Path3, view3.Path4, view3.Path5, view3.Path6, file.Filename);
                        string dest = Path.Combine(_options.DataRootDirectory, @"assets", @"webinar", file.Filename);
                        int n = _fileService.CopyFile(src, dest);
                        if (n != 0) url = _options.AssetsBaseURL + @"webinar" + "/" + file.Filename;
                        else url = "";

                        if (userId > 0)              // KC file. Put into log
                        {
                            KmActivityLog log = new KmActivityLog()
                            {
                                Action = "view",
                                UserId = userId,
                                FileId = view3.Id,
                                CreatedDate = DateTime.Now
                            };
                            _context.KmActivityLogs.Add(log);
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        url = string.Join("", new[] { _options.DocViewerBaseURL, view3.Id.ToString(), "-", userId.ToString() });
                    }

                    break;

                default:
                    break;
            }

            return url;
        }

        private string RenameFile(string filename, string newFilename, string path1, string path2, string path3, string path4, string path5, string path6)
        {
            string fullpath = Path.Combine(new string[] { path1, path2, path3, path4, path5, path6, filename});
            string newfullpath = Path.Combine(new string[] { path1, path2, path3, path4, path5, path6, newFilename });

            string fullpath2 = Path.Combine(new string[] { path1, path2, path3, path4, path5, filename });
            string newfullpath2 = Path.Combine(new string[] { path1, path2, path3, path4, path5, newFilename });

            string fullpath3 = Path.Combine(new string[] { path1, path2, path3, path4, filename });
            string newfullpath3 = Path.Combine(new string[] { path1, path2, path3, path4, newFilename });

            string fullpath4 = Path.Combine(new string[] { path1, path2, path3, filename });
            string newfullpath4 = Path.Combine(new string[] { path1, path2, path3, newFilename });

            string fullpath5 = Path.Combine(new string[] { path1, path2, filename });
            string newfullpath5 = Path.Combine(new string[] { path1, path2, newFilename });

            if (System.IO.File.Exists(fullpath))
            {
                System.IO.File.Move(fullpath, newfullpath);
                return newfullpath;
            }
            else if (System.IO.File.Exists(fullpath2))
            {
                System.IO.File.Move(fullpath2, newfullpath2);
                return newfullpath;
            }
            else if (System.IO.File.Exists(fullpath3))
            {
                System.IO.File.Move(fullpath3, newfullpath3);
                return newfullpath;
            }
            else if (System.IO.File.Exists(fullpath4))
            {
                System.IO.File.Move(fullpath4, newfullpath4);
                return newfullpath;
            }
            else if (System.IO.File.Exists(fullpath5))
            {
                System.IO.File.Move(fullpath5, newfullpath5);
                return newfullpath;
            }
            return null;
        }

        private string GetWebinarFileURL(string folder, string filename)
        {
            string baseURL = "https://www.onegml.com/";
            string url = baseURL;
            string[] strs = folder.Split(@"\");
            // ignore the first item in the array
            for (int i = 1; i < strs.Length; i++)
            {
                url += strs[i] + "/";
            }
            url += filename;
            return Uri.EscapeUriString(url);
        }
    }
}