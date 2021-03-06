﻿using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Internal;
using Xunit.v3;

namespace Xunit.Sdk
{
	/// <summary>
	/// Implementation of <see cref="IXunitTestCaseDiscoverer"/> that supports finding test cases
	/// on methods decorated with <see cref="FactAttribute"/>.
	/// </summary>
	public class FactDiscoverer : IXunitTestCaseDiscoverer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FactDiscoverer"/> class.
		/// </summary>
		/// <param name="diagnosticMessageSink">The message sink which receives <see cref="_DiagnosticMessage"/> messages.</param>
		public FactDiscoverer(_IMessageSink diagnosticMessageSink)
		{
			DiagnosticMessageSink = Guard.ArgumentNotNull(nameof(diagnosticMessageSink), diagnosticMessageSink);
		}

		/// <summary>
		/// Gets the message sink used to report <see cref="_DiagnosticMessage"/> messages.
		/// </summary>
		protected _IMessageSink DiagnosticMessageSink { get; }

		/// <summary>
		/// Creates a single <see cref="XunitTestCase"/> for the given test method.
		/// </summary>
		/// <param name="discoveryOptions">The discovery options to be used.</param>
		/// <param name="testMethod">The test method.</param>
		/// <param name="factAttribute">The attribute that decorates the test method.</param>
		/// <returns></returns>
		protected virtual IXunitTestCase CreateTestCase(
			_ITestFrameworkDiscoveryOptions discoveryOptions,
			ITestMethod testMethod,
			IAttributeInfo factAttribute)
		{
			Guard.ArgumentNotNull(nameof(discoveryOptions), discoveryOptions);
			Guard.ArgumentNotNull(nameof(testMethod), testMethod);
			Guard.ArgumentNotNull(nameof(factAttribute), factAttribute);

			var assemblyUniqueID = ComputeUniqueID(testMethod.TestClass.TestCollection.TestAssembly);
			var collectionUniqueID = ComputeUniqueID(assemblyUniqueID, testMethod.TestClass.TestCollection);
			var classUniqueID = ComputeUniqueID(collectionUniqueID, testMethod.TestClass);
			var methodUniqueID = ComputeUniqueID(classUniqueID, testMethod);

			return new XunitTestCase(
				assemblyUniqueID,
				collectionUniqueID,
				classUniqueID,
				methodUniqueID,
				DiagnosticMessageSink,
				discoveryOptions.MethodDisplayOrDefault(),
				discoveryOptions.MethodDisplayOptionsOrDefault(),
				testMethod
			);
		}

		/// <summary>
		/// Discover test cases from a test method. By default, if the method is generic, or
		/// it contains arguments, returns a single <see cref="ExecutionErrorTestCase"/>;
		/// otherwise, it returns the result of calling <see cref="CreateTestCase"/>.
		/// </summary>
		/// <param name="discoveryOptions">The discovery options to be used.</param>
		/// <param name="testMethod">The test method the test cases belong to.</param>
		/// <param name="factAttribute">The fact attribute attached to the test method.</param>
		/// <returns>Returns zero or more test cases represented by the test method.</returns>
		public virtual IEnumerable<IXunitTestCase> Discover(
			_ITestFrameworkDiscoveryOptions discoveryOptions,
			ITestMethod testMethod,
			IAttributeInfo factAttribute)
		{
			Guard.ArgumentNotNull(nameof(discoveryOptions), discoveryOptions);
			Guard.ArgumentNotNull(nameof(testMethod), testMethod);
			Guard.ArgumentNotNull(nameof(factAttribute), factAttribute);

			IXunitTestCase testCase;

			if (testMethod.Method.GetParameters().Any())
				testCase = ErrorTestCase(discoveryOptions, testMethod, "[Fact] methods are not allowed to have parameters. Did you mean to use [Theory]?");
			else if (testMethod.Method.IsGenericMethodDefinition)
				testCase = ErrorTestCase(discoveryOptions, testMethod, "[Fact] methods are not allowed to be generic.");
			else
				testCase = CreateTestCase(discoveryOptions, testMethod, factAttribute);

			return new[] { testCase };
		}

		ExecutionErrorTestCase ErrorTestCase(
			_ITestFrameworkDiscoveryOptions discoveryOptions,
			ITestMethod testMethod,
			string message)
		{
			var assemblyUniqueID = ComputeUniqueID(testMethod.TestClass.TestCollection.TestAssembly);
			var collectionUniqueID = ComputeUniqueID(assemblyUniqueID, testMethod.TestClass.TestCollection);
			var classUniqueID = ComputeUniqueID(collectionUniqueID, testMethod.TestClass);
			var methodUniqueID = ComputeUniqueID(classUniqueID, testMethod);

			return new ExecutionErrorTestCase(
				assemblyUniqueID,
				collectionUniqueID,
				classUniqueID,
				methodUniqueID,
				DiagnosticMessageSink,
				discoveryOptions.MethodDisplayOrDefault(),
				discoveryOptions.MethodDisplayOptionsOrDefault(),
				testMethod,
				message
			);
		}

		/// <summary>
		/// INTERNAL METHOD, DO NOT USE.
		/// </summary>
		public static string ComputeUniqueID(ITestAssembly testAssembly) =>
			UniqueIDGenerator.ForAssembly(testAssembly.Assembly.Name, testAssembly.Assembly.AssemblyPath, testAssembly.ConfigFileName);

		/// <summary>
		/// INTERNAL METHOD, DO NOT USE.
		/// </summary>
		public static string? ComputeUniqueID(
			string testCollectionUniqueID,
			ITestClass? testClass) =>
				UniqueIDGenerator.ForTestClass(testCollectionUniqueID, testClass?.Class?.Name);

		/// <summary>
		/// INTERNAL METHOD, DO NOT USE.
		/// </summary>
		public static string ComputeUniqueID(
			string assemblyUniqueID,
			ITestCollection testCollection) =>
				UniqueIDGenerator.ForTestCollection(assemblyUniqueID, testCollection.DisplayName, testCollection.CollectionDefinition?.Name);

		/// <summary>
		/// INTERNAL METHOD, DO NOT USE.
		/// </summary>
		public static string? ComputeUniqueID(
			string? classUniqueID,
			ITestMethod testMethod) =>
				UniqueIDGenerator.ForTestMethod(classUniqueID, testMethod.Method.Name);
	}
}
