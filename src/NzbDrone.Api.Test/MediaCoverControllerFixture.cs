using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaCover;
using NzbDrone.Test.Common;
using Readarr.Api.V1.MediaCovers;

namespace NzbDrone.Api.Test
{
    [TestFixture]
    public class MediaCoverControllerFixture : TestBase
    {
        private Mock<IAppFolderInfo> _appFolderInfo;
        private Mock<IDiskProvider> _diskProvider;
        private Mock<IAuthorService> _authorService;
        private Mock<IBookService> _bookService;
        private Mock<IMapCoversToLocal> _coverService;
        private Mock<IHttpClient> _httpClient;
        private MediaCoverController _subject;

        private Author _author;
        private Book _book;

        [SetUp]
        public void Setup()
        {
            _appFolderInfo = new Mock<IAppFolderInfo>();
            _diskProvider = new Mock<IDiskProvider>();
            _authorService = new Mock<IAuthorService>();
            _bookService = new Mock<IBookService>();
            _coverService = new Mock<IMapCoversToLocal>();
            _httpClient = new Mock<IHttpClient>();

            _author = new Author { Id = 140, Name = "Jem Calder" };
            _book = new Book { Id = 200, Title = "Reward System" };

            _authorService.Setup(s => s.GetAuthor(140)).Returns(_author);
            _bookService.Setup(s => s.GetBook(200)).Returns(_book);

            _subject = new MediaCoverController(
                _appFolderInfo.Object,
                _diskProvider.Object,
                _authorService.Object,
                _bookService.Object,
                _coverService.Object,
                _httpClient.Object);
        }

        [Test]
        public void update_author_cover_from_url_should_download_and_save()
        {
            var url = "https://example.com/cover.jpg";
            var imageData = Encoding.UTF8.GetBytes("fake-image-bytes");
            var httpResponse = new NzbDrone.Common.Http.HttpResponse(null, new HttpHeader(), imageData);

            _httpClient.Setup(c => c.Get(It.Is<NzbDrone.Common.Http.HttpRequest>(r => r.Url.FullUri == url)))
                       .Returns(httpResponse);

            var result = _subject.UpdateAuthorCoverFromUrl(140, new MediaCoverUrlRequest { Url = url });

            result.Should().BeOfType<OkObjectResult>();
            _coverService.Verify(c => c.SaveAuthorCover(_author, imageData, url), Times.Once());
        }

        [Test]
        public void update_author_cover_from_url_should_return_bad_request_when_url_is_empty()
        {
            var result = _subject.UpdateAuthorCoverFromUrl(140, new MediaCoverUrlRequest { Url = "" });

            result.Should().BeOfType<BadRequestObjectResult>();
            _coverService.Verify(c => c.SaveAuthorCover(It.IsAny<Author>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task upload_author_cover_should_save_uploaded_file_bytes()
        {
            var content = "fake-file-content";
            var fileName = "Jem-Calder.jpg";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var result = await _subject.UploadAuthorCover(140, formFile);

            result.Should().BeOfType<OkObjectResult>();
            _coverService.Verify(c => c.SaveAuthorCover(_author, It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == content), null), Times.Once());
        }

        [Test]
        public void update_book_cover_from_url_should_download_and_save()
        {
            var url = "https://example.com/book.jpg";
            var imageData = Encoding.UTF8.GetBytes("fake-book-bytes");
            var httpResponse = new NzbDrone.Common.Http.HttpResponse(null, new HttpHeader(), imageData);

            _httpClient.Setup(c => c.Get(It.Is<NzbDrone.Common.Http.HttpRequest>(r => r.Url.FullUri == url)))
                       .Returns(httpResponse);

            var result = _subject.UpdateBookCoverFromUrl(200, new MediaCoverUrlRequest { Url = url });

            result.Should().BeOfType<OkObjectResult>();
            _coverService.Verify(c => c.SaveBookCover(_book, imageData, url), Times.Once());
        }

        [Test]
        public async Task upload_book_cover_should_save_uploaded_file_bytes()
        {
            var content = "fake-book-cover-content";
            var fileName = "cover.jpg";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var result = await _subject.UploadBookCover(200, formFile);

            result.Should().BeOfType<OkObjectResult>();
            _coverService.Verify(c => c.SaveBookCover(_book, It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == content), null), Times.Once());
        }
    }
}
