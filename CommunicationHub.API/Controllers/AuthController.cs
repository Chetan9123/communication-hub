using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using CommunicationHub.API.DTOs;
using CommunicationHub.Infrastructure.Data;
using Adjuster = CommunicationHub.Domain.Entities.Adjuster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CommunicationHub.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly CommunicationHubDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(CommunicationHubDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// POST /api/auth/login
    /// Authenticates user and returns JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new AuthResponse { Success = false, Message = "Email and password are required" });

            // Find user by email
            var adjuster = _context.Adjusters.FirstOrDefault(a => a.Email == request.Email);

            if (adjuster == null || !BCrypt.Net.BCrypt.Verify(request.Password, adjuster.PasswordHash))
                return Unauthorized(new AuthResponse { Success = false, Message = "Invalid email or password" });

            // Check if user is active
            if (adjuster.IsActive == false)
                return Unauthorized(new AuthResponse { Success = false, Message = "Account is inactive" });

            // Generate JWT token
            var token = GenerateJwtToken(adjuster);

            var response = new AuthResponse
            {
                Success = true,
                Token = token,
                Message = "Login successful",
                User = new AdjusterDto
                {
                    AdjusterId = adjuster.AdjusterId,
                    FullName = adjuster.FullName,
                    Email = adjuster.Email,
                    Phone = adjuster.Phone,
                    IsActive = adjuster.IsActive
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse { Success = false, Message = $"Server error: {ex.Message}" });
        }
    }

    /// <summary>
    /// POST /api/auth/signup
    /// Creates new adjuster account
    /// </summary>
    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> Signup([FromBody] SignupRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new AuthResponse { Success = false, Message = "Email and password are required" });

            if (request.Password != request.ConfirmPassword)
                return BadRequest(new AuthResponse { Success = false, Message = "Passwords do not match" });

            if (request.Password.Length < 6)
                return BadRequest(new AuthResponse { Success = false, Message = "Password must be at least 6 characters" });

            // Check if email already exists
            var existingAdjuster = _context.Adjusters.FirstOrDefault(a => a.Email == request.Email);
            if (existingAdjuster != null)
                return Conflict(new AuthResponse { Success = false, Message = "Email already exists" });

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create new adjuster
            var newAdjuster = new Adjuster
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Adjusters.Add(newAdjuster);
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = GenerateJwtToken(newAdjuster);

            var response = new AuthResponse
            {
                Success = true,
                Token = token,
                Message = "Account created successfully",
                User = new AdjusterDto
                {
                    AdjusterId = newAdjuster.AdjusterId,
                    FullName = newAdjuster.FullName,
                    Email = newAdjuster.Email,
                    Phone = newAdjuster.Phone,
                    IsActive = newAdjuster.IsActive
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse { Success = false, Message = $"Server error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Generates JWT token for an adjuster
    /// </summary>
    private string GenerateJwtToken(Adjuster adjuster)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? "your-super-secret-key-change-this-in-production";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "CommunicationHub";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "CommunicationHubClient";
        var jwtExpiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adjuster.AdjusterId.ToString()),
            new Claim(ClaimTypes.Email, adjuster.Email ?? ""),
            new Claim(ClaimTypes.Name, adjuster.FullName ?? ""),
            new Claim("AdjusterId", adjuster.AdjusterId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
