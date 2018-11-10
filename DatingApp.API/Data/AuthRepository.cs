using System;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class AuthRepository : IAuthRepository
    {

        private readonly DataContext _conext;

        public AuthRepository(DataContext conext)
        {
            _conext = conext;
        }

#region "Global Functions"

        public async Task<User> Login(string userName, string password)
        {
            var user = await _conext.Users.FirstOrDefaultAsync(x => x.UserName == userName);
            if(user == null)
                return null;

            if(!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            return user;
        }

        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;

            //Simple: create a hashed password and also get a salt
            CreatePasswordHash(password, out passwordHash, out passwordSalt);
            
            //store
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _conext.Users.AddAsync(user);
            await _conext.SaveChangesAsync();

            return user;
        }

        public async Task<bool> UserExists(string userName)
        {
            if(await _conext.Users.AnyAsync(x => x.UserName == userName)) 
                return true;

            return false;
        }

#endregion

#region "Helper Functions"

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
            {
                using(var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
                {
                    var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                    for (int i=0; i<computedHash.Length; i++)
                    {
                        if(computedHash[i] != passwordHash[i]) return false;
                    }
                }
                return true;
            }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            //simple approach
            using(var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

#endregion
        
    }
}