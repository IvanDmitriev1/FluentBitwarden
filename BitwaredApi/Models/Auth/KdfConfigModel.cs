namespace BitwaredApi.Models.Auth;

public sealed record KdfConfigModel(
    KdfType Type,
    int Iterations,
    int? Memory = null,
    int? Parallelism = null);
