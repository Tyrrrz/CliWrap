using System;
using System.Globalization;
using CliWrap.Argument.ArgumentTypes;

// ReSharper disable once CheckNamespace
namespace CliWrap.Argument {

    /// <summary> Contains extension methods for <see cref="ArgumentBuilder" />. </summary>
    public static class ArgumentListExtensions {

        /// <summary> Appends the specified text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="text">    The text to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder Append(this ArgumentBuilder builder, string text) {
            builder?.Append(new TextArgument(text));
            return builder;
        }

        /// <summary> Prepend the specified text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="text">    The text to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder Prepend(this ArgumentBuilder builder, string text) {
            builder?.Prepend(new TextArgument(text));
            return builder;
        }

        /// <summary> Formats and appends the specified text to the argument builder. </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="format" /> or
        ///                                          <paramref name="args" /> is null. </exception>
        /// <exception cref="FormatException">       <paramref name="format" /> is invalid.-or- The index
        ///                                          of a format item is less than zero, or greater than
        ///                                          or equal to the length of the
        ///                                          <paramref name="args" /> array. </exception>
        /// <param name="builder"> The builder. </param>
        /// <param name="format">  A composite format string. </param>
        /// <param name="args">    An object array that contains zero or more objects to format. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder Append(this ArgumentBuilder builder, string format, params object[] args) {
            var text = string.Format(CultureInfo.InvariantCulture, format, args);
            return Append(builder, text);
        }

        /// <summary> Formats and prepends the specified text to the argument builder. </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="format" /> or
        ///                                          <paramref name="args" /> is null. </exception>
        /// <exception cref="FormatException">       <paramref name="format" /> is invalid.-or- The index
        ///                                          of a format item is less than zero, or greater than
        ///                                          or equal to the length of the
        ///                                          <paramref name="args" /> array. </exception>
        /// <param name="builder"> The builder. </param>
        /// <param name="format">  A composite format string. </param>
        /// <param name="args">    An object array that contains zero or more objects to format. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder Prepend(this ArgumentBuilder builder, string format, params object[] args) {
            var text = string.Format(CultureInfo.InvariantCulture, format, args);
            return Prepend(builder, text);
        }

        /// <summary> Quotes and appends the specified text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="text">    The text to be quoted and appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendQuoted(this ArgumentBuilder builder, string text) {
            builder?.Append(new QuotedArgument(new TextArgument(text)));
            return builder;
        }

        /// <summary> Quotes and prepends the specified text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="text">    The text to be quoted and prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependQuoted(this ArgumentBuilder builder, string text) {
            builder?.Prepend(new QuotedArgument(new TextArgument(text)));
            return builder;
        }

        /// <summary> Formats, quotes and appends the specified text to the argument builder. </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="format" /> or
        ///                                          <paramref name="args" /> is null. </exception>
        /// <exception cref="FormatException">       <paramref name="format" /> is invalid.-or- The index
        ///                                          of a format item is less than zero, or greater than
        ///                                          or equal to the length of the
        ///                                          <paramref name="args" /> array. </exception>
        /// <param name="builder"> The builder. </param>
        /// <param name="format">  A composite format string to be quoted and appended. </param>
        /// <param name="args">    An object array that contains zero or more objects to format. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendQuoted(this ArgumentBuilder builder, string format, params object[] args) {
            var text = string.Format(CultureInfo.InvariantCulture, format, args);
            return AppendQuoted(builder, text);
        }

        /// <summary> Formats, quotes and prepends the specified text to the argument builder. </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="format" /> or
        ///                                          <paramref name="args" /> is null. </exception>
        /// <exception cref="FormatException">       <paramref name="format" /> is invalid.-or- The index
        ///                                          of a format item is less than zero, or greater than
        ///                                          or equal to the length of the
        ///                                          <paramref name="args" /> array. </exception>
        /// <param name="builder"> The builder. </param>
        /// <param name="format">  A composite format string to be quoted and prepended. </param>
        /// <param name="args">    An object array that contains zero or more objects to format. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependQuoted(this ArgumentBuilder builder, string format, params object[] args) {
            var text = string.Format(CultureInfo.InvariantCulture, format, args);
            return PrependQuoted(builder, text);
        }

        /// <summary> Quotes and appends the specified argument to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="argument"> The argument to be quoted and appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendQuoted(this ArgumentBuilder builder, IProcessArgument argument) {
            builder?.Append(new QuotedArgument(argument));
            return builder;
        }

        /// <summary> Quotes and prepends the specified argument to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="argument"> The argument to be quoted and prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependQuoted(this ArgumentBuilder builder, IProcessArgument argument) {
            builder?.Prepend(new QuotedArgument(argument));
            return builder;
        }

        /// <summary> Appends the specified secret text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="text">    The secret text to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSecret(this ArgumentBuilder builder, string text) {
            builder?.Append(new SecretArgument(new TextArgument(text)));
            return builder;
        }

        /// <summary> Prepends the specified secret text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="text">    The secret text to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSecret(this ArgumentBuilder builder, string text) {
            builder?.Prepend(new SecretArgument(new TextArgument(text)));
            return builder;
        }

        /// <summary> Formats and appends the specified secret text to the argument builder. </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="format" /> or
        ///                                          <paramref name="args" /> is null. </exception>
        /// <exception cref="FormatException">       <paramref name="format" /> is invalid.-or- The index
        ///                                          of a format item is less than zero, or greater than
        ///                                          or equal to the length of the
        ///                                          <paramref name="args" /> array. </exception>
        /// <param name="builder"> The builder. </param>
        /// <param name="format">  A composite format string for the secret text to be appended. </param>
        /// <param name="args">    An object array that contains zero or more objects to format. </param>
        /// <returns> The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///           chained. </returns>
        /// <returns> The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///           chained. </returns>
        public static ArgumentBuilder AppendSecret(this ArgumentBuilder builder, string format, params object[] args) {
            var text = string.Format(CultureInfo.InvariantCulture, format, args);
            return AppendSecret(builder, text);
        }

        /// <summary> Formats and prepend the specified secret text to the argument builder. </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="format" /> or
        ///                                          <paramref name="args" /> is null. </exception>
        /// <exception cref="FormatException">       <paramref name="format" /> is invalid.-or- The index
        ///                                          of a format item is less than zero, or greater than
        ///                                          or equal to the length of the
        ///                                          <paramref name="args" /> array. </exception>
        /// <param name="builder"> The builder. </param>
        /// <param name="format">  A composite format string for the secret text to be prepended. </param>
        /// <param name="args">    An object array that contains zero or more objects to format. </param>
        /// <returns> The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///           chained. </returns>
        /// <returns> The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///           chained. </returns>
        public static ArgumentBuilder PrependSecret(this ArgumentBuilder builder, string format, params object[] args) {
            var text = string.Format(CultureInfo.InvariantCulture, format, args);
            return PrependSecret(builder, text);
        }

        /// <summary> Appends the specified secret text to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="argument"> The secret argument to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSecret(this ArgumentBuilder builder, IProcessArgument argument) {
            builder?.Append(new SecretArgument(argument));
            return builder;
        }

        /// <summary> Prepend the specified secret text to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="argument"> The secret argument to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSecret(this ArgumentBuilder builder, IProcessArgument argument) {
            builder?.Prepend(new SecretArgument(argument));
            return builder;
        }

        /// <summary> Quotes and appends the specified secret text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="text">    The secret text to be quoted and appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendQuotedSecret(this ArgumentBuilder builder, string text) {
            builder?.AppendQuoted(new SecretArgument(new TextArgument(text)));
            return builder;
        }

        /// <summary> Quotes and prepends the specified secret text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="text">    The secret text to be quoted and prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependQuotedSecret(this ArgumentBuilder builder, string text) {
            builder?.PrependQuoted(new SecretArgument(new TextArgument(text)));
            return builder;
        }

        /// <summary>
        ///     Formats, quotes and appends the specified secret text to the argument builder.
        /// </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="format" /> or
        ///                                          <paramref name="args" /> is null. </exception>
        /// <exception cref="FormatException">       <paramref name="format" /> is invalid.-or- The index
        ///                                          of a format item is less than zero, or greater than
        ///                                          or equal to the length of the
        ///                                          <paramref name="args" /> array. </exception>
        /// <param name="builder"> The builder. </param>
        /// <param name="format">  A composite format string for the secret text to be quoted and
        ///                        appended. </param>
        /// <param name="args">    An object array that contains zero or more objects to format. </param>
        /// <returns> The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///           chained. </returns>
        /// <returns> The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///           chained. </returns>
        public static ArgumentBuilder AppendQuotedSecret(this ArgumentBuilder builder, string format, params object[] args) {
            var text = string.Format(CultureInfo.InvariantCulture, format, args);
            return AppendQuotedSecret(builder, text);
        }

        /// <summary>
        ///     Formats, quotes and prepends the specified secret text to the argument builder.
        /// </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="format" /> or
        ///                                          <paramref name="args" /> is null. </exception>
        /// <exception cref="FormatException">       <paramref name="format" /> is invalid.-or- The index
        ///                                          of a format item is less than zero, or greater than
        ///                                          or equal to the length of the
        ///                                          <paramref name="args" /> array. </exception>
        /// <param name="builder"> The builder. </param>
        /// <param name="format">  A composite format string for the secret text to be quoted and
        ///                        prepended. </param>
        /// <param name="args">    An object array that contains zero or more objects to format. </param>
        /// <returns> The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///           chained. </returns>
        /// <returns> The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///           chained. </returns>
        public static ArgumentBuilder PrependQuotedSecret(this ArgumentBuilder builder, string format, params object[] args) {
            var text = string.Format(CultureInfo.InvariantCulture, format, args);
            return PrependQuotedSecret(builder, text);
        }

        /// <summary> Quotes and appends the specified secret text to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="argument"> The secret argument to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendQuotedSecret(this ArgumentBuilder builder, IProcessArgument argument) {
            builder?.AppendQuoted(new SecretArgument(argument));
            return builder;
        }

        /// <summary> Quotes and prepends the specified secret text to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="argument"> The secret argument to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependQuotedSecret(this ArgumentBuilder builder, IProcessArgument argument) {
            builder?.PrependQuoted(new SecretArgument(argument));
            return builder;
        }

        /// <summary> Appends the specified switch to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="switch">  The switch preceding the text. </param>
        /// <param name="text">    The text to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitch(this ArgumentBuilder builder, string @switch, string text) {
            return AppendSwitch(builder, @switch, " ", text);
        }

        /// <summary> Prepend the specified switch to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="switch">  The switch preceding the text. </param>
        /// <param name="text">    The text to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitch(this ArgumentBuilder builder, string @switch, string text) {
            return PrependSwitch(builder, @switch, " ", text);
        }

        /// <summary> Appends the specified switch to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="text">      The text to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitch(this ArgumentBuilder builder, string @switch, string separator, string text) {
            builder?.Append(new SwitchArgument(@switch, new TextArgument(text), separator));
            return builder;
        }

        /// <summary> Prepend the specified switch to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="text">      The text to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitch(this ArgumentBuilder builder, string @switch, string separator, string text) {
            builder?.Prepend(new SwitchArgument(@switch, new TextArgument(text), separator));
            return builder;
        }

        /// <summary> Quotes and appends the specified text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="switch">  The switch preceding the text. </param>
        /// <param name="text">    The text to be quoted and appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitchQuoted(this ArgumentBuilder builder, string @switch, string text) {
            return AppendSwitchQuoted(builder, @switch, " ", text);
        }

        /// <summary> Quotes and prepends the specified text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="switch">  The switch preceding the text. </param>
        /// <param name="text">    The text to be quoted and prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitchQuoted(this ArgumentBuilder builder, string @switch, string text) {
            return PrependSwitchQuoted(builder, @switch, " ", text);
        }

        /// <summary> Quotes and appends the specified text to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="text">      The text to be quoted and appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitchQuoted(this ArgumentBuilder builder, string @switch, string separator, string text) {
            builder?.Append(new SwitchArgument(@switch, new QuotedArgument(new TextArgument(text)), separator));
            return builder;
        }

        /// <summary> Quotes and prepends the specified text to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="text">      The text to be quoted and prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitchQuoted(this ArgumentBuilder builder, string @switch, string separator, string text) {
            builder?.Prepend(new SwitchArgument(@switch, new QuotedArgument(new TextArgument(text)), separator));
            return builder;
        }

        /// <summary> Quotes and appends the specified argument to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="switch">   The switch preceding the text. </param>
        /// <param name="argument"> The argument to be quoted and appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitchQuoted(this ArgumentBuilder builder, string @switch, IProcessArgument argument) {
            return AppendSwitchQuoted(builder, @switch, " ", argument);
        }

        /// <summary> Quotes and prepends the specified argument to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="switch">   The switch preceding the text. </param>
        /// <param name="argument"> The argument to be quoted and prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitchQuoted(this ArgumentBuilder builder, string @switch, IProcessArgument argument) {
            return PrependSwitchQuoted(builder, @switch, " ", argument);
        }

        /// <summary> Quotes and appends the specified argument to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="argument">  The argument to be quoted and appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitchQuoted(this ArgumentBuilder builder, string @switch, string separator, IProcessArgument argument) {
            builder?.Append(new SwitchArgument(@switch, new QuotedArgument(argument), separator));
            return builder;
        }

        /// <summary> Quotes and prepends the specified argument to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="argument">  The argument to be quoted and prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitchQuoted(this ArgumentBuilder builder, string @switch, string separator, IProcessArgument argument) {
            builder?.Prepend(new SwitchArgument(@switch, new QuotedArgument(argument), separator));
            return builder;
        }

        /// <summary> Appends the specified secret text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="switch">  The switch preceding the text. </param>
        /// <param name="text">    The secret text to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitchSecret(this ArgumentBuilder builder, string @switch, string text) {
            return AppendSwitchSecret(builder, @switch, " ", text);
        }

        /// <summary> Prepend the specified secret text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="switch">  The switch preceding the text. </param>
        /// <param name="text">    The secret text to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitchSecret(this ArgumentBuilder builder, string @switch, string text) {
            return PrependSwitchSecret(builder, @switch, " ", text);
        }

        /// <summary> Appends the specified secret text to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="text">      The secret text to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitchSecret(this ArgumentBuilder builder, string @switch, string separator, string text) {
            builder?.Append(new SwitchArgument(@switch, new SecretArgument(new TextArgument(text)), separator));
            return builder;
        }

        /// <summary> Prepend the specified secret text to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="text">      The secret text to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitchSecret(this ArgumentBuilder builder, string @switch, string separator, string text) {
            builder?.Prepend(new SwitchArgument(@switch, new SecretArgument(new TextArgument(text)), separator));
            return builder;
        }

        /// <summary> Appends the specified secret text to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="switch">   The switch preceding the text. </param>
        /// <param name="argument"> The secret argument to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitchSecret(this ArgumentBuilder builder, string @switch, IProcessArgument argument) {
            return AppendSwitchSecret(builder, @switch, " ", argument);
        }

        /// <summary> Prepend the specified secret text to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="switch">   The switch preceding the text. </param>
        /// <param name="argument"> The secret argument to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitchSecret(this ArgumentBuilder builder, string @switch, IProcessArgument argument) {
            return PrependSwitchSecret(builder, @switch, " ", argument);
        }

        /// <summary> Appends the specified secret text to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="argument">  The secret argument to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitchSecret(this ArgumentBuilder builder, string @switch, string separator, IProcessArgument argument) {
            builder?.Append(new SwitchArgument(@switch, new SecretArgument(argument), separator));
            return builder;
        }

        /// <summary> Prepend the specified secret text to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="argument">  The secret argument to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitchSecret(this ArgumentBuilder builder, string @switch, string separator, IProcessArgument argument) {
            builder?.Prepend(new SwitchArgument(@switch, new SecretArgument(argument), separator));
            return builder;
        }

        /// <summary> Quotes and appends the specified secret text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="switch">  The switch preceding the text. </param>
        /// <param name="text">    The secret text to be quoted and appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitchQuotedSecret(this ArgumentBuilder builder, string @switch, string text) {
            return AppendSwitchQuotedSecret(builder, @switch, " ", text);
        }

        /// <summary> Quotes and prepend the specified secret text to the argument builder. </summary>
        /// <param name="builder"> The builder. </param>
        /// <param name="switch">  The switch preceding the text. </param>
        /// <param name="text">    The secret text to be quoted and prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitchQuotedSecret(this ArgumentBuilder builder, string @switch, string text) {
            return PrependSwitchQuotedSecret(builder, @switch, " ", text);
        }

        /// <summary> Quotes and appends the specified secret text to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="text">      The secret text to be quoted and appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendSwitchQuotedSecret(this ArgumentBuilder builder, string @switch, string separator, string text) {
            builder?.AppendSwitchQuoted(@switch, separator, new SecretArgument(new TextArgument(text)));
            return builder;
        }

        /// <summary> Quotes and prepends the specified secret text to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="text">      The secret text to be quoted and prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependSwitchQuotedSecret(this ArgumentBuilder builder, string @switch, string separator, string text) {
            builder?.PrependSwitchQuoted(@switch, separator, new SecretArgument(new TextArgument(text)));
            return builder;
        }

        /// <summary> Quotes and appends the specified secret text to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="switch">   The switch preceding the text. </param>
        /// <param name="argument"> The secret argument to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendQuotedSecret(this ArgumentBuilder builder, string @switch, IProcessArgument argument) {
            return AppendQuotedSecret(builder, @switch, " ", argument);
        }

        /// <summary> Quotes and prepends the specified secret text to the argument builder. </summary>
        /// <param name="builder">  The builder. </param>
        /// <param name="switch">   The switch preceding the text. </param>
        /// <param name="argument"> The secret argument to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependQuotedSecret(this ArgumentBuilder builder, string @switch, IProcessArgument argument) {
            return PrependQuotedSecret(builder, @switch, " ", argument);
        }

        /// <summary> Quotes and appends the specified secret text to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="argument">  The secret argument to be appended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder AppendQuotedSecret(this ArgumentBuilder builder, string @switch, string separator, IProcessArgument argument) {
            builder?.AppendSwitchQuoted(@switch, separator, new SecretArgument(argument));
            return builder;
        }

        /// <summary> Quotes and prepend the specified secret text to the argument builder. </summary>
        /// <param name="builder">   The builder. </param>
        /// <param name="switch">    The switch preceding the text. </param>
        /// <param name="separator"> The separator between the switch and argument. </param>
        /// <param name="argument">  The secret argument to be prepended. </param>
        /// <returns>
        ///     The same <see cref="ArgumentBuilder"/> instance so that multiple calls can be
        ///     chained.
        /// </returns>
        public static ArgumentBuilder PrependQuotedSecret(this ArgumentBuilder builder, string @switch, string separator, IProcessArgument argument) {
            builder?.PrependSwitchQuoted(@switch, separator, new SecretArgument(argument));
            return builder;
        }

        /// <summary>
        ///     Indicates whether a <see cref="ArgumentBuilder"/> is null or renders empty.
        /// </summary>
        /// <param name="builder"> The builder. </param>
        /// <returns>
        ///     <c>true</c> if <paramref name="builder"/> refers to a null or empty
        ///     <see cref="ArgumentBuilder"/>;
        ///     <c>false</c> if the <paramref name="builder"/>refers to non null or empty
        ///     <see cref="ArgumentBuilder"/>
        /// </returns>
        public static bool IsNullOrEmpty(this ArgumentBuilder builder) {
            return builder == null || builder.Count == 0 || string.IsNullOrEmpty(builder.Render());
        }

        /// <summary>
        ///     Copies all the arguments of the source <see cref="ArgumentBuilder"/> to target
        ///     <see cref="ArgumentBuilder"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"> Thrown when one or more required arguments are null. </exception>
        /// <param name="source"> The argument builder to copy from. </param>
        /// <param name="target"> The argument builder to copy to. </param>
        public static void CopyTo(this ArgumentBuilder source, ArgumentBuilder target) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }

            foreach (var token in source) {
                target.Append(token);
            }
        }

    }
}