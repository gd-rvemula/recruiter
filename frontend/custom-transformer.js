/**
 * Custom Jest transformer for handling import.meta.env
 * This transformer replaces import.meta.env references with process.env
 */

// Custom transformer that replaces import.meta.env.XXX with process.env.XXX
export default {
  process(sourceText) {
    return {
      code: sourceText.replace(/import\.meta\.env\.(\w+)/g, 'process.env.$1'),
    };
  },
};
