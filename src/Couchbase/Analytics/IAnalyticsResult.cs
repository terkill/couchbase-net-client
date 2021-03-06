using System;
using System.Collections.Generic;

#nullable enable

namespace Couchbase.Analytics
{
    /// <summary>
    /// Results of an analytics query.
    /// </summary>
    /// <typeparam name="T">Type of each row in the result.</typeparam>
    public interface IAnalyticsResult<out T> : IDisposable, IAsyncEnumerable<T>, IServiceResult
    {
        /// <summary>
        /// The results of the query as a <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <remarks>
        /// In most cases, the rows may be enumerated only once. If it's necessary to enumerate more than
        /// once, use <see cref="System.Linq.AsyncEnumerable.ToListAsync{T}(IAsyncEnumerable{T}, System.Threading.CancellationToken)"/> to convert to a list.
        /// ToListAsync can also be used to enumerate with a synchronous foreach loop in C# 7.
        /// </remarks>
        IAsyncEnumerable<T> Rows { get; }

        /// <summary>
        /// Gets the meta data associated with the analytics result.
        /// </summary>
        AnalyticsMetaData? MetaData { get; }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2017 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion
