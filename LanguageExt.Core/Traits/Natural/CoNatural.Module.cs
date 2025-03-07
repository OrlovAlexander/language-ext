namespace LanguageExt.Traits;

public static class CoNatural
{
    /// <summary>
    /// Co-natural transformation
    /// </summary>
    /// <remarks>
    /// If functor `map` operations transform the bound-values within the structure, then
    /// natural-transformations transform the structure itself.
    /// </remarks>
    /// <remarks>
    /// Functors are referenced, because that's the true definition in category-theory, but
    /// there is no requirement in language-ext for FA or GA to be functors.  It is just typically
    /// true that FA and GA will also be functors.
    /// </remarks>
    /// <param name="fa">Functor to transform</param>
    /// <typeparam name="F">Source functor type</typeparam>
    /// <typeparam name="G">Target functor type</typeparam>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns>Transformed functor</returns>
    public static K<F, A> transform<F, G, A>(K<G, A> fa)
        where F : CoNatural<F, G> =>
        F.CoTransform(fa);
}
