<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:src="http://www.srcML.org/srcML/src"
    xmlns:func="http://exslt.org/functions"
    xmlns:str="http://exslt.org/strings"
        extension-element-prefixes="func"
    version="1.0">
<!-- Default identity copy. XSLT works by calling rules that best match the node it's looking at. The identity copy essentially only matches things that nothing else ever matches (because it basically says to match everything and any other template is more contrained than that) so this is a catch all rule and the other templates are essentially specializations-->
<xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
</xsl:template>

<xsl:template match="src:decl_stmt">
    <src:test>TestPassed</src:test>
 </xsl:template>
</xsl:stylesheet>