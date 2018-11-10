using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            _config = config;
            _repo = repo;
        }

        //due to [ApiController], we don't need to put [FromBody] 
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegDto userForRegDto)
        {
            //due to [ApiController], we don't need to put the following line
            //if(!ModelState.IsValid) return BadRequest(ModelState);

            userForRegDto.Username = userForRegDto.Username.ToLower();

            if (await _repo.UserExists(userForRegDto.Username))
                return BadRequest("User name already exists.");

            var userToCreate = new User { UserName = userForRegDto.Username };
            var createdUser = await _repo.Register(userToCreate, userForRegDto.Password);

            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var dbUser = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);
            if (dbUser == null)
                return Unauthorized();

            //build a token 

            //will contain UserId and UserName as claims
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
                new Claim(ClaimTypes.Name, dbUser.UserName)
            };

            //key to sign the token, will be hashed. 
            //Encode into byte array 
            //The key will come from app settings, inject configuration via the constructor
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            //signing credentials using HmacSha512Signature, takes our above key and hashes it.
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            //create security token descripter which wil contain claims, expiry date for the token and the signin credentials
            var tokenDescriptor = new SecurityTokenDescriptor 
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1), //this is variable
                SigningCredentials = creds
            };
            
            //token handler
            var tokenHandler = new JwtSecurityTokenHandler();

            //create a token using the token handler and pass in the token descriptor
            //this will contain our JWT token we want to return to our clients
            var token = tokenHandler.CreateToken(tokenDescriptor);

            //send the Ok response with token object 
            return Ok(new { token = tokenHandler.WriteToken(token) });

        }
    }
}