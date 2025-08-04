using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using GiteaSyncTool.Encryption;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GiteaSyncTool
{

    public class BaseSettings
    {
        public GithubExportSettings GithubExportSettings { get; set; }
        public GiteaSyncSettings GiteaSyncSettings { get; set; }
        public double ImportDelay { get; set; } = 2.5;

    }

    public class GiteaSyncSettings
    {
        public string GiteaUrl { get; set; }
        public string GiteaUsername { get; set; }
        [Encrypt]
        public string GiteaToken { get; set; }
        public string Owner { get; set; }
        public bool OwnerIsOrg { get; set; } = false;
        public bool IsPrivate { get; set; }
    }
    public class GithubExportSettings
    {
        public string GithubUsername { get; set; }
        [Encrypt]
        public string GithubToken { get; set; }
        public bool IsMirror { get; set; }
        public bool MigrateLFS { get; set; }
        public string LFSEndpoint { get; set; }
        public bool Wiki { get; set; }
        public bool Labels { get; set; }
        public bool Issues { get; set; }
        public bool PullRequests { get; set; }
        public bool Releases { get; set; }
        public bool Milestones { get; set; }

    }

    public class GithubRepoInfo
    {
        public string GithubFullUrl { get; set; }
        public string RepoName { get; set; }        
        public string Description { get; set; }
        public string Homepage { get; set; }
        public string Language { get; set; }
        public bool HasWiki { get; set; }
        public bool HasIssues { get; set; }
        public bool HasDownloads { get; set; }
        /// <summary>
        /// Size In KB
        /// </summary>
        public long RepositorySize { get; set; }
        public bool IsArchived { get; set; }
        public string[] Topics { get; set; }
        public bool IsVisible { get; set; }
    }

    public partial class MigrateRepoOptions
    {
        /// <summary>
        /// Defines Service
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ServiceEnum
        {
            [EnumMember(Value = "git")]
            Git = 1,

            [EnumMember(Value = "github")]
            Github = 2,

            [EnumMember(Value = "gitea")]
            Gitea = 3,

            [EnumMember(Value = "gitlab")]
            Gitlab = 4,

            [EnumMember(Value = "gogs")]
            Gogs = 5,

            [EnumMember(Value = "onedev")]
            Onedev = 6,

            [EnumMember(Value = "gitbucket")]
            Gitbucket = 7,

            [EnumMember(Value = "codebase")]
            Codebase = 8
        }

        [JsonProperty("clone_addr", Required = Required.Always)]
        public string CloneAddr { get; set; }

        [JsonProperty("repo_name", Required = Required.Always)]
        public string RepoName { get; set; }



        [JsonProperty("service")]
        public ServiceEnum? Service { get; set; }
        
        [JsonProperty("auth_password")]
        public string AuthPassword { get; set; }


        [JsonProperty("auth_token")]
        public string AuthToken { get; set; }


        [JsonProperty("auth_username")]
        public string AuthUsername { get; set; }


        [JsonProperty("description")]
        public string Description { get; set; }


        [JsonProperty("issues")]
        public bool Issues { get; set; }


        [JsonProperty("labels")]
        public bool Labels { get; set; }


        [JsonProperty("lfs")]
        public bool Lfs { get; set; }


        [JsonProperty("lfs_endpoint")]
        public string LfsEndpoint { get; set; }


        [JsonProperty("milestones")]
        public bool Milestones { get; set; }

        [JsonProperty("mirror")]
        public bool Mirror { get; set; }


        [JsonProperty("mirror_interval")]
        public string MirrorInterval { get; set; }


        [JsonProperty("private")]
        public bool Private { get; set; }


        [JsonProperty("pull_requests")]
        public bool PullRequests { get; set; }


        [JsonProperty("releases")]
        public bool Releases { get; set; }        

        [JsonProperty("repo_owner")]
        public string RepoOwner { get; set; }

        [JsonProperty("wiki")]
        public bool Wiki { get; set; }

        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }
        
    }

}
