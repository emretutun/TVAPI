using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TVAPI.Application.Scenes;

namespace TVAPI.Controllers.Scenes
{
    // PLAYLIST UÇLARI (kaynak: kanal veritabanı; tbl_playlist_group, tbl_saved_scenes)
    //   GET /api/v1/scenes/playlists?categoryId=   kategoriye göre playlist'ler
    //   GET /api/v1/scenes/playlists/{id}          playlist'in sahneleri ve kullandıkları şablonlar
    [ApiController]
    [Route("api/v1/scenes/playlists")]
    public class PlaylistsController : ControllerBase
    {
        private const int MaxCategories = 20;

        private readonly IPlaylistService _playlistService;

        public PlaylistsController(IPlaylistService playlistService)
        {
            _playlistService = playlistService;
        }

        /// <summary>
        /// Verilen kategorilerdeki playlist'leri ada göre sıralı döner.
        /// <c>categoryId</c> birden çok kez yazılabilir: <c>?categoryId=2&amp;categoryId=3</c>.
        /// </summary>
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<PlaylistDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<PlaylistDto>>> GetByCategories(
            [FromQuery, Required, MinLength(1), MaxLength(MaxCategories)] int[] categoryId,
            CancellationToken cancellationToken)
        {
            return Ok(await _playlistService.GetByCategoriesAsync(categoryId, cancellationToken));
        }

        /// <summary>
        /// Playlist'in kayıtlı sahnelerini liste sırasıyla ve bu sahnelerin kullandığı şablonları döner.
        /// Playlist yoksa 404; playlist var ama boşsa <c>scenes</c> boş liste gelir.
        /// </summary>
        [HttpGet("{playlistId:int:min(1)}")]
        [ProducesResponseType<PlaylistContentDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PlaylistContentDto>> GetContent(int playlistId, CancellationToken cancellationToken)
        {
            var content = await _playlistService.GetContentAsync(playlistId, cancellationToken);

            return content is null ? NotFound() : Ok(content);
        }
    }
}
