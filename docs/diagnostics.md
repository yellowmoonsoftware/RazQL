# Generator diagnostics

RazQL reports these errors at build time for interfaces marked with `[RazQLMapper]`. Fix the mapper declaration or its template before rebuilding. Each rule ID links here from IDE diagnostics.

## RAZQL001

**Open generic mapper.** A decorated interface, including one nested in a generic type, must be closed. Declare a non-generic mapper interface for each concrete type.

## RAZQL002

**Static mapper method.** Query methods must be instance methods. Remove `static`.

## RAZQL003

**Generic mapper method.** Query methods cannot declare type parameters. Use concrete criteria and result types on a non-generic method.

## RAZQL004

**Default implementation.** Every mapper method must be implemented by the generator. Remove its body.

## RAZQL005

**Invalid return shape.** Return `Task<T>` by value, with `T` using a supported result shape. Avoid `ref` returns and non-Task wrappers.

## RAZQL006

**Invalid parameter shape.** Declare exactly one by-value criteria parameter followed by a by-value `CancellationToken`.

## RAZQL007

**Unsupported result type.** Use a scalar or object result, or `IEnumerable<T>` for multiple rows. Nested tasks, async streams, enumerators, and concrete collection shapes are not supported.

## RAZQL008

**Query name collision.** Two methods in one mapper produce the same template-name candidate. Give enough overloaded methods an explicit `RazQLQueryTemplateSourceAttribute.TemplateName` to make the names unique.

## RAZQL009

**Blank attribute name.** A configured mapper group name or template name cannot be empty or whitespace. Provide a name or omit the override.

## RAZQL010

**Invalid loader type.** The configured loader must be a concrete, closed implementation of `ITemplateSourceLoader`.

## RAZQL011

**Non-public member.** All members of a mapper interface must be public. Remove restrictive accessibility or move the member elsewhere.

## RAZQL012

**Unsupported member.** Mapper interfaces contain query methods only. Move properties, events, and other members to another interface.

## RAZQL013

**Inherited method conflict.** A method hides or duplicates one inherited by the mapper. Rename or remove the conflicting declaration.

## RAZQL014

**Mapper group collision.** Two decorated interfaces produce the same group-name candidate. Give at least one mapper an explicit `[RazQLMapper(Name = "...")]` name.

## RAZQL015

**Inaccessible mapper.** The generated top-level implementation must be able to reference the interface and its containing types. Increase their accessibility or move the mapper.

## RAZQL016

**Invalid criteria type.** The criteria type must be usable as a generic type argument. Replace pointer or ref-like criteria with a regular type.

## RAZQL017

**Template not found.** Add a `.sql.cshtml` file at one of the reported conventional paths, or adjust the mapper's template location or name. Built-in file and resource loaders are checked during compilation.

## RAZQL018

**Ambiguous template.** More than one file matches the mapper method's candidate paths. Remove an extra file or set an explicit template name.

## RAZQL019

**Template configuration mismatch.** For resource loading, mark the file `EmbeddedResource` and give it the expected manifest name. For file-system loading, mark it `Content`, copy it to output, and use an expected target path. The diagnostic explains which metadata differs.

## RAZQL020

**Conflicting source attributes.** Apply only one template-source attribute to the mapper interface or method. A method-level attribute may override interface-level configuration.

## RAZQL021

**Blank inline query.** `[RazQLQuery("...")]` requires non-whitespace SQL template source. Supply the query text or use an external template.
