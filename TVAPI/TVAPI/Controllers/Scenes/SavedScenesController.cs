using Microsoft.AspNetCore.Mvc;
using TVAPI.Application.Scenes;

namespace TVAPI.Controllers.Scenes
{
    // KAYITLI SAHNE UÇLARI (kaynak: kanal veritabanı; tbl_saved_scenes)
    //   GET /api/v1/scenes/saved/{id}   tek kayıtlı sahne, şablonuyla birlikte
    //
    // Bir playlist'in bütün sahneleri için: GET /api/v1/scenes/playlists/{id}
    [ApiController]
    [Route("api/v1/scenes/saved")]
    public class SavedScenesController : ControllerBase
    {
        private readonly IPlaylistService _playlistService;

        public SavedScenesController(IPlaylistService playlistService)
        {
            _playlistService = playlistService;
        }

        /// <summary>Tek kayıtlı sahneyi şablonuyla döner; sahne ya da şablonu yoksa 404.</summary>
        [HttpGet("{savedSceneId:long:min(1)}")]
        [ProducesResponseType<SavedSceneDetailDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SavedSceneDetailDto>> GetById(long savedSceneId, CancellationToken cancellationToken)
        {
            var scene = await _playlistService.GetSceneAsync(savedSceneId, cancellationToken);

            return scene is null ? NotFound() : Ok(scene);
        }
    }
}
