using Xunit;

namespace CliWrap.Tests;

// Don't run these tests in parallel because they rely on observing global state
[CollectionDefinition(nameof(NonParallelCollection), DisableParallelization = true)]
public class NonParallelCollection;
