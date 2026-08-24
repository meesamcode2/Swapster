using Swapster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Swapster.Services
{
    public class AiRecommendationService
    {
        private readonly HttpClient _httpClient;

        public AiRecommendationService(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<int>>
            GetRecommendedSkillIdsAsync(
                IEnumerable<UserWantedSkill> wantedSkills,
                IEnumerable<UserSkill> offeredSkills,
                int topN = 6)
        {
            var wantedList =
                wantedSkills?
                    .Where(skill =>
                        !string.IsNullOrWhiteSpace(
                            skill.SkillName
                        ))
                    .ToList()
                ?? new List<UserWantedSkill>();

            var offeredList =
                offeredSkills?
                    .Where(skill =>
                        !string.IsNullOrWhiteSpace(
                            skill.SkillName
                        ))
                    .ToList()
                ?? new List<UserSkill>();

            if (!wantedList.Any() ||
                !offeredList.Any())
            {
                return new List<int>();
            }

            var payload =
                new AiRecommendationRequest
                {
                    WantedSkills =
                        wantedList
                            .Select(skill =>
                                new AiWantedSkillDto
                                {
                                    SkillName =
                                        skill.SkillName,

                                    DesiredLevel =
                                        skill.DesiredLevel
                                })
                            .ToList(),

                    OfferedSkills =
                        offeredList
                            .Select(skill =>
                                new AiOfferedSkillDto
                                {
                                    SkillId =
                                        skill.SkillId,

                                    UserId =
                                        skill.UserId,

                                    SkillName =
                                        skill.SkillName,

                                    Description =
                                        skill.Description
                                        ?? string.Empty,

                                    ProficiencyLevel =
                                        skill.ProficiencyLevel
                                })
                            .ToList(),

                    TopN =
                        Math.Clamp(
                            topN,
                            1,
                            20
                        )
                };

            try
            {
                using var response =
                    await _httpClient.PostAsJsonAsync(
                        "recommend",
                        payload
                    );

                if (!response.IsSuccessStatusCode)
                {
                    return new List<int>();
                }

                var result =
                    await response.Content
                        .ReadFromJsonAsync<
                            AiRecommendationResponse
                        >();

                if (result == null ||
                    !result.Success ||
                    result.Recommendations == null)
                {
                    return new List<int>();
                }

                return result.Recommendations
                    .Where(item =>
                        item.SkillId > 0)
                    .OrderByDescending(item =>
                        item.FinalScore)
                    .Select(item =>
                        item.SkillId)
                    .Distinct()
                    .Take(payload.TopN)
                    .ToList();
            }
            catch
            {
                // Dashboard already has a safe database fallback.
                // If Flask is offline, return no AI IDs so the
                // controller can continue without crashing.
                return new List<int>();
            }
        }


        private class AiRecommendationRequest
        {
            [JsonPropertyName("wantedSkills")]
            public List<AiWantedSkillDto>
                WantedSkills
            { get; set; }
                = new();

            [JsonPropertyName("offeredSkills")]
            public List<AiOfferedSkillDto>
                OfferedSkills
            { get; set; }
                = new();

            [JsonPropertyName("topN")]
            public int TopN { get; set; }
        }


        private class AiWantedSkillDto
        {
            [JsonPropertyName("skillName")]
            public string SkillName { get; set; }
                = string.Empty;

            [JsonPropertyName("desiredLevel")]
            public int DesiredLevel { get; set; }
        }


        private class AiOfferedSkillDto
        {
            [JsonPropertyName("skillId")]
            public int SkillId { get; set; }

            [JsonPropertyName("userId")]
            public int UserId { get; set; }

            [JsonPropertyName("skillName")]
            public string SkillName { get; set; }
                = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; set; }
                = string.Empty;

            [JsonPropertyName("proficiencyLevel")]
            public int ProficiencyLevel { get; set; }
        }


        private class AiRecommendationResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("count")]
            public int Count { get; set; }

            [JsonPropertyName("recommendations")]
            public List<AiRecommendationItem>
                Recommendations
            { get; set; }
                = new();
        }


        private class AiRecommendationItem
        {
            [JsonPropertyName("skillId")]
            public int SkillId { get; set; }

            [JsonPropertyName("semanticScore")]
            public double SemanticScore { get; set; }

            [JsonPropertyName("levelScore")]
            public double LevelScore { get; set; }

            [JsonPropertyName("finalScore")]
            public double FinalScore { get; set; }
        }
    }
}