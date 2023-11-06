﻿namespace PartialWidgetPage;

public class PartialWidgetPageHelper : IPartialWidgetPageHelper
{
    private readonly IHttpContextRetriever mContextRetriever;
    private readonly IWebPageDataContextInitializer mDataContextInitializer;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IPageBuilderDataContextRetriever mPageBuilderDataContextRetriever;
    private readonly IPreferredLanguageRetriever mPreferredLanguageRetriever;
    private readonly IWebPageDataContextRetriever mWebPageDataContextRetriever;

    private readonly IInfoProvider<WebPageItemInfo> mMWebPageItemInfoProvider;

    public PartialWidgetPageHelper(
        IPageBuilderDataContextRetriever pageBuilderDataContextRetriever,
        IWebPageDataContextRetriever webP,
        IHttpContextRetriever httpContextRetriever,
        IInfoProvider<WebPageItemInfo> webPageItemInfoProvider,
        IPreferredLanguageRetriever preferredLanguageRetriever,
        IHttpContextAccessor httpContextAccessor, 
        IWebPageDataContextInitializer dataContextInitializer)
    {
        mPageBuilderDataContextRetriever = pageBuilderDataContextRetriever;
        mWebPageDataContextRetriever = webP;
        mContextRetriever = httpContextRetriever;
        mMWebPageItemInfoProvider = webPageItemInfoProvider;
        mPreferredLanguageRetriever = preferredLanguageRetriever;
        mHttpContextAccessor = httpContextAccessor;
        mDataContextInitializer = dataContextInitializer;
    }

    public PreservedPageBuilderContext GetCurrentContext()
    {
        var context = mContextRetriever.GetContext();
        var pageBuilderContext = mPageBuilderDataContextRetriever.Retrieve();

        IRoutedWebPage page = null;
        if (mWebPageDataContextRetriever.TryRetrieve(out var webPageContext)) page = webPageContext.WebPage;

        return new PreservedPageBuilderContext
        {
            PageBuilderFeature = context.Kentico().GetFeature<IPageBuilderFeature>(),
            PageBuilderContext = pageBuilderContext,
            Page = page
        };
    }

    public void ChangeContext()
    {
        ChangeContextInternal();
    }

    public void ChangeContext(int id)
    {
        var wbi = mMWebPageItemInfoProvider.Get(id);
        var language = mPreferredLanguageRetriever.Get();

        var routedPage = new PartialWidgetPageDataContextWebPage(wbi.WebPageItemID, language);

        ChangeContextInternal(routedPage);
    }

    public void RestoreContext(PreservedPageBuilderContext previousContext)
    {
        var context = mContextRetriever.GetContext();
        // Restore

        context.Kentico().SetFeature(previousContext.PageBuilderFeature);

        context.Items[PAGE_BUILDER_DATA_CONTEXT_KEY] = previousContext.PageBuilderContext;
        context.Items[WEB_PAGE_DATA_CONTEXT_KEY] = previousContext.Page;
    }

    public string LayoutIfEditMode(string layout)
    {
        return mHttpContextAccessor.HttpContext.PartialWidgetPage().LayoutIfEditMode(layout);
    }

    public string LayoutIfNotAjax(string layout)
    {
        return mHttpContextAccessor.HttpContext.PartialWidgetPage().LayoutIfNotAjax(layout);
    }

    private void ChangeContextInternal(IRoutedWebPage webPage = null)
    {
        var context = mContextRetriever.GetContext();
        var options = context.Kentico().PageBuilder().Options;

        var pageBuilderContext = new PageBuilderDataContext
        {
            Options = options,
            EditMode = false
        };

        context.Items[PAGE_BUILDER_DATA_CONTEXT_KEY] = pageBuilderContext;

        if (webPage == null)
            context.Items[WEB_PAGE_DATA_CONTEXT_KEY] = null;
        else
            mDataContextInitializer.Initialize(webPage);

        //Store new cloned context to change edit mode for rendered page builder interface
        context.Kentico().SetFeature<IPageBuilderFeature>(new ClonedPageBuilderFeature(context, pageBuilderContext));
    }
}