﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AuthzDemoWeb.Data;
using ModelLibrary;
using Microsoft.AspNetCore.Authorization;
using AuthorizationLibrary;
using Microsoft.Extensions.Logging;
using DataFiltering;
using AuthorizationLibrary.Common;

namespace AuthzDemoWeb.Pages.Articles
{
    public class IndexModel : PageModel
    {
        private readonly AuthzDemoWeb.Data.ApplicationDbContext _context;
        private readonly IAuthorizationService _auth;
        private readonly ILogger _logger;

        public IndexModel(IAuthorizationService authorizationService,
            AuthzDemoWeb.Data.ApplicationDbContext context,
            ILoggerFactory loggerFactory)
        {
            _auth = authorizationService;
            _context = context;
            _logger = loggerFactory.CreateLogger<IndexModel>();
        }

        public IList<Article> Article { get; set; }

        public bool IsAuthorized { get; set; }

        public async Task OnGet1Async()
        {
            //var authResult = await _auth.AuthorizeAsync(User, new Article(), ArticlePolicies.ListArticles);
            //if (!authResult.Succeeded)
            //{
            //    IsAuthorized = false;
            //    Article = new List<Article>();
            //    return;
            //}

            Article = await _context.Articles.ToListAsync();

            var res = Article.Select(a => new
            {
                Article = a,
                AuthResultTask = _auth.AuthorizeAsync(User, a,
                    ArticlePolicies.ListArticles1),
            }).ToArray();

            var results = await Task.WhenAll(res.Select(t => t.AuthResultTask).ToArray());

            Article = res
                .Where(r => r.AuthResultTask.Result.Succeeded)
                .Select(r => r.Article)
                .ToList();

            var denied = res
                .Where(r => !r.AuthResultTask.Result.Succeeded)
                .Select(r => new FailureDescriptor()
                {
                    Article = r.Article,
                    Failure = r.AuthResultTask.Result.Failure,
                })
                .ToList();

            ArticlesHelper.LogFailure(_logger, denied);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var authResult = await _auth.AuthorizeAsync(User,
                    ArticlePolicies.ListArticles2);
            if (!authResult.Succeeded)
            {
                ArticlesHelper.LogFailure(_logger, authResult.Failure);
                return new ChallengeResult();
            }

            var userMaturity = MaturityHelper.GetMaturity(User);
            var userName = User.Identity.Name;

            Article = await _context.Articles
                .EnforceAgeAndOwner(userName, userMaturity)
                .ToListAsync();

            return Page();

            //var denied = res
            //    .Where(r => !r.AuthResultTask.Result.Succeeded)
            //    .Select(r => new FailureDescriptor()
            //    {
            //        Article = r.Article,
            //        Failure = r.AuthResultTask.Result.Failure,
            //    })
            //    .ToList();

            //ArticlesHelper.LogFailure(_logger, denied);
        }


    }
}
