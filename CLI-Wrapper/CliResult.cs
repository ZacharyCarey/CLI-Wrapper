using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLI_Wrapper {

    public class CliResult {

        public Exception? Exception { get; }
        public int ExitCode { get; }
        public IReadOnlyList<string> OutputData { get; }
        public IReadOnlyList<string> ErrorData { get; }

        internal CliResult(Exception? exception, int exitCode, IReadOnlyList<string> stdOutput, IReadOnlyList<string> errorOutput) {
            this.Exception = exception;
            this.ExitCode = exitCode;
            this.OutputData = stdOutput;
            this.ErrorData = errorOutput;
        }

    }
}
