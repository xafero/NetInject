namespace NetInject.API
{
    /// <summary>
    /// The entry point to patch methods
    /// </summary>
    public interface IInvocationHandler
    {
        /// <summary>
        /// Patches a method invocation on an instance
        /// </summary>
        /// <param name="real">the used object</param>
        /// <param name="method">the invoked method</param>
        /// <param name="args">given arguments</param>
        /// <returns>recalculated result</returns>
        /// <exception cref="System.NotImplementedException">if not interesting</exception>
        object Invoke(object real, string method, object[] args);
    }
}