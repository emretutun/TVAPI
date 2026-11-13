using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TVAPI.Application.News;

namespace TVAPI.Controllers.News
{
    // HABER UÇLARI (kaynak: haber veritabanı; tbl_yayin_saatleri, tbl_records, tbl_news_saved_scene, viz_saved_scenes)
    //   GET /api/v1/news/rundowns?date=                             günün yayın akışları
    //   GET /api/v1/news/stories?date=&directoryId=&timeSlotId=     bir akıştaki haberler
    //   GET /api/v1/news/stories/{id}/scenes                        habere bağlı kayıtlı sahneler ve şablonları
    //
    // Yalnızca güncel tablolar okunur; arşivdeki (eski tarihli) yayınlar kapsam dışındadır.
    [ApiController]
    [Route("api/v1/news")]
    public class NewsController : ControllerBase
    {
        private readonly INewsService _newsService;

        public NewsController(INewsService newsService)
        {
            _newsService = newsService;
        }

        /// <summary>
        /// Bir günün yayın akışlarını yayın saatine göre sıralı döner. <c>date</c> verilmezse bugün kullanılır.
        /// Örnek: <c>?date=2026-11-14</c>
        /// </summary>
        [HttpGet("rundowns")]
        [ProducesResponseType<IReadOnlyList<RundownDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<RundownDto>>> GetRundowns(
            [FromQuery] DateOnly? date,
            CancellationToken cancellationToken)
        {
            return Ok(await _newsService.GetRundownsAsync(date, cancellationToken));
        }

        /// <summary>
        /// Bir yayın akışındaki haberleri akış sırasıyla döner. Üç değer de akış listesinden alınır:
        /// <c>date</c>, <c>directoryId</c> ve <c>timeSlotId</c>.
        /// </summary>
        [HttpGet("stories")]
        [ProducesResponseType<IReadOnlyList<StoryDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<StoryDto>>> GetStories(
            [FromQuery, Required] DateOnly? date,
            [FromQuery, Required, Range(1, int.MaxValue)] int? directoryId,
            [FromQuery, Required, Range(1, int.MaxValue)] int? timeSlotId,
            CancellationToken cancellationToken)
        {
            return Ok(await _newsService.GetStoriesAsync(date!.Value, directoryId!.Value, timeSlotId!.Value, cancellationToken));
        }

        /// <summary>
        /// Habere bağlı kayıtlı sahneleri sırasıyla ve kullandıkları şablonları döner.
        /// Habere bağlı sahne yoksa listeler boş gelir.
        /// </summary>
        [HttpGet("stories/{storyId:long:min(1)}/scenes")]
        [ProducesResponseType<StoryScenesDto>(StatusCodes.Status200OK)]
        public async Task<ActionResult<StoryScenesDto>> GetStoryScenes(long storyId, CancellationToken cancellationToken)
        {
            return Ok(await _newsService.GetStoryScenesAsync(storyId, cancellationToken));
        }
    }
}
