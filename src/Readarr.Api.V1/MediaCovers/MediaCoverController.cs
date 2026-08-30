using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaCover;
using Readarr.Http;

namespace Readarr.Api.V1.MediaCovers
{
    public class MediaCoverUrlRequest
    {
        public string Url { get; set; }
    }

    [V1ApiController]
    public class MediaCoverController : Controller
    {
        private static readonly Regex RegexResizedImage = new Regex(@"-\d+(?=\.(jpg|png|gif)$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly IContentTypeProvider _mimeTypeProvider;
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IMapCoversToLocal _coverService;
        private readonly IHttpClient _httpClient;

        public MediaCoverController(IAppFolderInfo appFolderInfo,
                                    IDiskProvider diskProvider,
                                    IAuthorService authorService,
                                    IBookService bookService,
                                    IMapCoversToLocal coverService,
                                    IHttpClient httpClient)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _mimeTypeProvider = new FileExtensionContentTypeProvider();
            _authorService = authorService;
            _bookService = bookService;
            _coverService = coverService;
            _httpClient = httpClient;
        }

        [HttpGet(@"author/{authorId:int}/{filename:regex((.+)\.(jpg|png|gif))}")]
        public IActionResult GetAuthorMediaCover(int authorId, string filename)
        {
            var filePath = Path.Combine(_appFolderInfo.GetAppDataPath(), "MediaCover", authorId.ToString(), filename);

            if (!_diskProvider.FileExists(filePath) || _diskProvider.GetFileSize(filePath) == 0)
            {
                // Return the full sized image if someone requests a non-existing resized one.
                // TODO: This code can be removed later once everyone had the update for a while.
                var basefilePath = RegexResizedImage.Replace(filePath, "");
                if (basefilePath == filePath || !_diskProvider.FileExists(basefilePath))
                {
                    return NotFound();
                }

                filePath = basefilePath;
            }

            return PhysicalFile(filePath, GetContentType(filePath));
        }

        [HttpGet(@"book/{bookId:int}/{filename:regex((.+)\.(jpg|png|gif))}")]
        public IActionResult GetBookMediaCover(int bookId, string filename)
        {
            var filePath = Path.Combine(_appFolderInfo.GetAppDataPath(), "MediaCover", "Books", bookId.ToString(), filename);

            if (!_diskProvider.FileExists(filePath) || _diskProvider.GetFileSize(filePath) == 0)
            {
                // Return the full sized image if someone requests a non-existing resized one.
                // TODO: This code can be removed later once everyone had the update for a while.
                var basefilePath = RegexResizedImage.Replace(filePath, "");
                if (basefilePath == filePath || !_diskProvider.FileExists(basefilePath))
                {
                    return NotFound();
                }

                filePath = basefilePath;
            }

            return PhysicalFile(filePath, GetContentType(filePath));
        }

        [HttpPost(@"author/{authorId:int}")]
        public async Task<IActionResult> UpdateAuthorCover(int authorId, [FromBody] MediaCoverUrlRequest urlRequest = null, IFormFile file = null)
        {
            var author = _authorService.GetAuthor(authorId);
            if (author == null)
            {
                return NotFound();
            }

            byte[] bytes = null;
            string remoteUrl = null;

            if (file != null && file.Length > 0)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            else if (Request.HasFormContentType && Request.Form.Files.Count > 0)
            {
                var formFile = Request.Form.Files[0];
                if (formFile.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await formFile.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }
            }
            else if (urlRequest != null && !string.IsNullOrWhiteSpace(urlRequest.Url))
            {
                remoteUrl = urlRequest.Url.Trim();
                try
                {
                    var response = _httpClient.Get(new HttpRequest(remoteUrl));
                    bytes = response.ResponseData;
                }
                catch (Exception ex)
                {
                    return BadRequest($"Failed to download image from URL: {ex.Message}");
                }
            }

            if (bytes == null || bytes.Length == 0)
            {
                return BadRequest("No image file or valid URL provided");
            }

            _coverService.SaveAuthorCover(author, bytes, remoteUrl);
            return Ok(new { success = true });
        }

        [HttpPost(@"book/{bookId:int}")]
        public async Task<IActionResult> UpdateBookCover(int bookId, [FromBody] MediaCoverUrlRequest urlRequest = null, IFormFile file = null)
        {
            var book = _bookService.GetBook(bookId);
            if (book == null)
            {
                return NotFound();
            }

            byte[] bytes = null;
            string remoteUrl = null;

            if (file != null && file.Length > 0)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            else if (Request.HasFormContentType && Request.Form.Files.Count > 0)
            {
                var formFile = Request.Form.Files[0];
                if (formFile.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await formFile.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }
            }
            else if (urlRequest != null && !string.IsNullOrWhiteSpace(urlRequest.Url))
            {
                remoteUrl = urlRequest.Url.Trim();
                try
                {
                    var response = _httpClient.Get(new HttpRequest(remoteUrl));
                    bytes = response.ResponseData;
                }
                catch (Exception ex)
                {
                    return BadRequest($"Failed to download image from URL: {ex.Message}");
                }
            }

            if (bytes == null || bytes.Length == 0)
            {
                return BadRequest("No image file or valid URL provided");
            }

            _coverService.SaveBookCover(book, bytes, remoteUrl);
            return Ok(new { success = true });
        }

        private string GetContentType(string filePath)
        {
            if (!_mimeTypeProvider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return contentType;
        }
    }
}
