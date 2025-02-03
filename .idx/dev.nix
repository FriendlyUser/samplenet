{ pkgs, ... }: {
  # Which nixpkgs channel to use.
  channel = "stable-24.05"; # or "unstable"

  # List all system packages you want available in your workspace.
  # Here we add the .NET SDK along with openai-whisper, ffmpeg_6-full, and yt-dlp.
  packages = [
    pkgs.dotnet-sdk_8
    pkgs.openai-whisper
    pkgs.ffmpeg_6-full
  ];

  # Set any environment variables you need in the workspace.
  env = {
    # Example: SOME_VAR = "value";
  };

  idx = {
    # Install IDE extensions automatically.
    # Use the fully qualified extension IDs from https://open-vsx.org/.
    extensions = [
      "muhammad-sammy.csharp"
      "rangav.vscode-thunder-client"
    ];

    # (Optional) Enable and configure app previews.
    # This example uses the web preview to run your .NET app.
    previews = {
      enable = true;
      previews = {
        web = {
          command = [
            "dotnet"
            "watch"
            "--urls=http://localhost:3000"
          ];
          manager = "web";
          # Optionally, specify the directory that contains your web app:
          # cwd = "path/to/app";
        };
      };
    };

    # Configure workspace behavior. For example, run your server when the workspace starts.
    workspace = {
      onStart = {
        run-server = "dotnet watch --urls=http://localhost:3000";
      };
    };
  };
}
